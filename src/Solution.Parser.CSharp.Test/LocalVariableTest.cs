using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    [TestClass]
    public class LocalVariableTest
    {
        private static Method ParseMethod(string body)
        {
            return ParseCode($$"""
                public class Holder
                {
                    public async Task Go(object input, string[] items)
                    {
                {{body}}
                    }
                }
                """).Classes.Single().Methods.Single();
        }

        [TestMethod]
        public void Declaration_Is_Reported_With_Type_Initializer_And_Location()
        {
            var method = ParseMethod("""
                        var count = 3;
                        string? name = null;
                        const int Max = 10;
                """);

            Assert.That.AreEqual<string>(["count", "name", "Max"],
                                         method.LocalVariables.Select(v => v.Name),
                                         because: "every declared variable must be visible to a rule, in source order",
                                         fix: "Collect each VariableDeclaratorSyntax inside the body");

            var count = method.LocalVariables[0];

            Assert.That.IsTrue(count.IsVar,
                               because: "a rule about explicit types has to tell var apart",
                               fix: "Derive IsVar from the TypeReference of the declaration");

            Assert.That.AreEqual("3",
                                 count.Initializer,
                                 because: "the value after = is what a rule about initialisation inspects",
                                 fix: "Read the value of the EqualsValueClauseSyntax");

            Assert.That.AreEqual("var count = 3;",
                                 count.SourceCode,
                                 because: "a finding quotes the whole declaration, like a field does",
                                 fix: "Use the LocalDeclarationStatementSyntax as source");

            Assert.That.AreEqual(5,
                                 count.Location.StartLine,
                                 because: "a finding must point at the variable",
                                 fix: "Give the variable the location of its declarator");

            Assert.That.IsTrue(method.LocalVariables[1].IsNullable,
                               because: "string? is a nullable declaration",
                               fix: "Derive IsNullable from the TypeReference");

            Assert.That.IsTrue(method.LocalVariables[2].IsConst,
                               because: "a const local is a constant, not a variable a rule may want renamed",
                               fix: "Read IsConst from the LocalDeclarationStatementSyntax");
        }

        /// <summary>The QueryProvider rule had to search statements for "using" as text.</summary>
        [TestMethod]
        public void Using_Is_Recognised_With_The_Calls_Of_The_Initializer()
        {
            var method = ParseMethod("""
                        using var provider = factory.GetQueryProvider();
                        await using (var connection = Open()) { }
                        var leaked = factory.GetQueryProvider();
                """);

            var provider = method.LocalVariables.Named("provider").Single();

            Assert.That.IsTrue(provider.IsUsing && !provider.IsAwaitUsing,
                               because: "a rule about disposal has to see using var",
                               fix: "Read the using keyword of the LocalDeclarationStatementSyntax");

            Assert.That.AreEqual("GetQueryProvider",
                                 provider.Invocations.Single().Name,
                                 because: "what a variable is created from is the call in its initializer",
                                 fix: "Run ToInvocations over the initializer value");

            var connection = method.LocalVariables.Named("connection").Single();

            Assert.That.IsTrue(connection.IsUsing && connection.IsAwaitUsing && connection.Kind == LocalVariableKind.UsingStatement,
                               because: "the using statement disposes its variable as well",
                               fix: "Treat the declaration of a UsingStatementSyntax as using");

            Assert.That.IsFalse(method.LocalVariables.Named("leaked").Single().IsUsing,
                                because: "this is the variable the rule must report",
                                fix: "Only set IsUsing for a using keyword");
        }

        [TestMethod]
        public void Every_Kind_Of_Declaration_Is_Found()
        {
            var method = ParseMethod("""
                        for (var i = 0; i < 3; i++) { }
                        foreach (var item in items) { }
                        foreach (var (key, value) in pairs) { }
                        try { } catch (InvalidOperationException ex) { }
                        if (input is string text) { }
                        if (input is { } any) { }
                        int.TryParse("1", out var number);
                        var (first, second) = (1, 2);
                        switch (input) { case var other: break; }
                """);

            Assert.That.AreEqual<string>(["i", "item", "key", "value", "ex", "text", "any", "number", "first", "second", "other"],
                                         method.LocalVariables.Select(v => v.Name),
                                         because: "a variable is a variable however it is declared",
                                         fix: "Collect declarators, foreach and catch identifiers and single variable designations");

            Assert.That.AreEqual<LocalVariableKind>([LocalVariableKind.For,
                                                     LocalVariableKind.ForEach,
                                                     LocalVariableKind.ForEach,
                                                     LocalVariableKind.ForEach,
                                                     LocalVariableKind.Catch,
                                                     LocalVariableKind.Pattern,
                                                     LocalVariableKind.Pattern,
                                                     LocalVariableKind.OutArgument,
                                                     LocalVariableKind.Deconstruction,
                                                     LocalVariableKind.Deconstruction,
                                                     LocalVariableKind.Pattern],
                                                    method.LocalVariables.Select(v => v.Kind),
                                                    because: "the kind tells a rule where the variable comes from",
                                                    fix: "Derive the kind from the node that holds the declaration");

            Assert.That.AreEqual("InvalidOperationException ex",
                                 method.LocalVariables.Named("ex").Single().SourceCode,
                                 because: "the source of a catch variable is its declaration, without the parentheses",
                                 fix: "Slice the source from the type to the identifier");

            Assert.That.AreEqual("string",
                                 method.LocalVariables.Named("text").Single().Type,
                                 because: "a pattern variable has the type of its pattern",
                                 fix: "Read the type of the DeclarationPatternSyntax");

            Assert.That.AreEqual("var",
                                 method.LocalVariables.Named("key").Single().Type,
                                 because: "each part of var (a, b) is declared with var",
                                 fix: "Walk up through the ParenthesizedVariableDesignationSyntax to the DeclarationExpressionSyntax");

            Assert.That.AreEqual(string.Empty,
                                 method.LocalVariables.Named("any").Single().Type,
                                 because: "is { } x writes no type",
                                 fix: "Return TypeReference.None for a RecursivePatternSyntax without a type");
        }

        [TestMethod]
        public void Lambda_Variables_Belong_To_The_Member_Local_Functions_Report_Their_Own()
        {
            var method = ParseMethod("""
                        Func<int> make = () => { var inLambda = 1; return inLambda; };
                        int Helper() { var inLocal = 2; return inLocal; }
                """);

            Assert.That.AreEqual<string>(["make", "inLambda"],
                                         method.LocalVariables.Select(v => v.Name),
                                         because: "a lambda has no model of its own, a local function has",
                                         fix: "Stop the search at a LocalFunctionStatementSyntax, as for the calls");

            Assert.That.AreEqual<string>(["inLocal"],
                                         method.LocalFunctions.Single().LocalVariables.Select(v => v.Name),
                                         because: "the local function reports the variables declared in it",
                                         fix: "Pass the local function body through ToLocalVariables");
        }

        [TestMethod]
        public void Shared_Declaration_Reports_Each_Variable()
        {
            var variables = ParseMethod("        int a = 1, b = 2;").LocalVariables;

            Assert.That.AreEqual<string>(["a", "b"],
                                         variables.Select(v => v.Name),
                                         because: "both are variables",
                                         fix: "Emit one LocalVariable per VariableDeclaratorSyntax");

            Assert.That.All(variables,
                            v => v.IsPartOfMultiVariableDeclaration && v.Type == "int",
                            "is part of a shared int declaration",
                            because: "a rule banning shared declarations must recognise them",
                            fix: "Set IsPartOfMultiVariableDeclaration when the declaration holds more than one variable");
        }

        [TestMethod]
        public void All_Local_Variables_Of_A_File()
        {
            var tree = ParseCode("""
                public class Outer
                {
                    public Outer() { var inCtor = 1; }
                    public void Go() => int.TryParse("1", out var inExpression);
                    public class Inner { void Run() { var inNested = 2; } }
                }
                """);

            Assert.That.AreEqual<string>(["inCtor", "inExpression", "inNested"],
                                         tree.LocalVariables.Select(v => v.Name).Order(),
                                         because: "a file wide rule starts from every variable of every member",
                                         fix: "Walk every type and member with a body in CSharpSyntaxTree.LocalVariables");
        }
    }
}
