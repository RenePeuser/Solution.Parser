using System;
using System.IO;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// One row per kind the parser maps from a Roslyn node. The mappings are switch arms, and an arm
    /// that maps to the wrong kind goes unnoticed until a rule misses something, so every arm has a row.
    /// </summary>
    [TestClass]
    public class SyntaxKindCoverageTest
    {
        private static Method ParseMethod(string body, string modifiers = "public")
        {
            return ParseCode($$"""
                public class Holder
                {
                    {{modifiers}} void Go()
                    {
                {{body}}
                    }
                }
                """).Classes.Single().Methods.Single();
        }

        private static Expression ParseInitializer(string expression)
        {
            return ParseMethod($"object r = {expression};").LocalVariables.Single(v => v.Name == "r").InitializerExpression!;
        }

        [TestMethod]
        [DataRow("'c'", ExpressionKind.CharLiteral)]
        [DataRow("default", ExpressionKind.DefaultLiteral)]
        [DataRow("\"abc\"u8", ExpressionKind.StringLiteral)]
        [DataRow("(int)x", ExpressionKind.Cast)]
        [DataRow("x as string", ExpressionKind.As)]
        [DataRow("default(int)", ExpressionKind.Default)]
        [DataRow("x ? 1 : 2", ExpressionKind.Conditional)]
        [DataRow("items[0]", ExpressionKind.ElementAccess)]
        [DataRow("(1, 2)", ExpressionKind.Tuple)]
        [DataRow("p with { X = 1 }", ExpressionKind.With)]
        [DataRow("this", ExpressionKind.This)]
        [DataRow("new { Id = 1 }", ExpressionKind.AnonymousObject)]
        [DataRow("new[] { 1 }", ExpressionKind.ArrayCreation)]
        [DataRow("stackalloc int[3]", ExpressionKind.ArrayCreation)]
        [DataRow("[1, 2]", ExpressionKind.CollectionExpression)]
        [DataRow("-x", ExpressionKind.Unary)]
        [DataRow("!x", ExpressionKind.Unary)]
        [DataRow("x++", ExpressionKind.Unary)]
        [DataRow("a + b", ExpressionKind.Binary)]
        [DataRow("a ?? b", ExpressionKind.Binary)]
        [DataRow("x is string", ExpressionKind.Is)]
        [DataRow("x is { } y", ExpressionKind.Is)]
        [DataRow("x = 1", ExpressionKind.Assignment)]
        [DataRow("await t", ExpressionKind.Await)]
        [DataRow("x switch { _ => 1 }", ExpressionKind.Switch)]
        [DataRow("nameof(x)", ExpressionKind.NameOf)]
        [DataRow("((x))", ExpressionKind.Identifier)]
        [DataRow("Foo<int>", ExpressionKind.Identifier)]
        [DataRow("int.MaxValue", ExpressionKind.MemberAccess)]
        [DataRow("x?.Name", ExpressionKind.MemberAccess)]
        [DataRow("x?.Go()", ExpressionKind.Invocation)]
        [DataRow("new Foo()", ExpressionKind.ObjectCreation)]
        [DataRow("delegate { }", ExpressionKind.Lambda)]
        public void Every_Expression_Kind_Is_Classified(string expression, ExpressionKind expected)
        {
            Assert.That.AreEqual(expected,
                                 ParseInitializer(expression).Kind,
                                 because: $"{expression} is a {expected}, and a rule matches on the kind rather than on the text",
                                 fix: "Check the arm for this node in ExpressionSyntaxExtensions.Classify");
        }

        [TestMethod]
        public void Expression_Details_Are_Kept()
        {
            var cast = ParseInitializer("(int)x");
            var asExpression = ParseInitializer("x as string");
            var defaultOf = ParseInitializer("default(Guid)");

            Assert.That.IsTrue(cast.TypeReference?.Name == "int" && cast.Operand?.Name == "x",
                               because: "a cast names the type and keeps what it converts",
                               fix: "Set TypeReference and Operand for a CastExpressionSyntax");

            Assert.That.IsTrue(asExpression.TypeReference?.Name == "string" && asExpression.Operand?.Name == "x",
                               because: "an as expression names the type and keeps what it converts",
                               fix: "Set TypeReference and Operand for an AsExpression");

            Assert.That.AreEqual("Guid",
                                 defaultOf.TypeReference?.Name,
                                 because: "default(T) names the type",
                                 fix: "Set TypeReference for a DefaultExpressionSyntax");

            Assert.That.AreEqual('c',
                                 ParseInitializer("'c'").LiteralValue,
                                 because: "a char literal has the char as value",
                                 fix: "Take the Value of the literal token");

            Assert.That.AreEqual("abc",
                                 ParseInitializer("\"abc\"u8").StringValue,
                                 because: "a UTF-8 literal has the text as value, without the suffix",
                                 fix: "Take the ValueText of a Utf8StringLiteralExpression");

            var throwArm = ParseMethod("object r = x switch { _ => throw new InvalidOperationException() };").SwitchExpressions.Single().Arms.Single();

            Assert.That.AreEqual(ExpressionKind.Throw,
                                 throwArm.Result.Kind,
                                 because: "a throw used as an expression is a kind of its own",
                                 fix: "Map ThrowExpressionSyntax to ExpressionKind.Throw");
        }

        [TestMethod]
        [DataRow("while (x) { }", StatementKind.While)]
        [DataRow("do { } while (x);", StatementKind.Do)]
        [DataRow("for (;;) { break; }", StatementKind.Break)]
        [DataRow("while (x) { continue; }", StatementKind.Continue)]
        [DataRow("goto end; end: return;", StatementKind.Goto)]
        [DataRow("goto end; end: return;", StatementKind.Labeled)]
        [DataRow(";", StatementKind.Empty)]
        [DataRow("checked { }", StatementKind.Checked)]
        [DataRow("unsafe { }", StatementKind.Unsafe)]
        [DataRow("fixed (int* p = arr) { }", StatementKind.Fixed)]
        [DataRow("lock (x) { }", StatementKind.Lock)]
        [DataRow("using (x) { }", StatementKind.Using)]
        [DataRow("int Local() => 1;", StatementKind.LocalFunction)]
        public void Every_Statement_Kind_Is_Reported(string body, StatementKind expected)
        {
            var kinds = ParseMethod(body, "public unsafe").AllStatements.Select(s => s.Kind);

            Assert.That.Contains(kinds,
                                 expected,
                                 because: $"{body} holds a {expected} statement, which a rule may look for",
                                 fix: "Check the arm for this statement in StatementBuilder.Create");
        }

        [TestMethod]
        public void Statement_Details_Are_Kept()
        {
            var whileLoop = ParseMethod("while (retries > 0) { retries--; }").Loops.Single();
            var doLoop = ParseMethod("do { retries--; } while (retries > 0);").Loops.Single();
            var labeled = ParseMethod("goto end; end: Cleanup();").AllStatements.Single(s => s.Kind == StatementKind.Labeled);
            var fixedStatement = ParseMethod("fixed (int* p = arr) { Use(p); }", "public unsafe").AllStatements.Single(s => s.Kind == StatementKind.Fixed);

            Assert.That.AreEqual("retries > 0",
                                 whileLoop.Condition?.SourceCode,
                                 because: "the condition of a while loop is what a rule about endless loops reads",
                                 fix: "Set Condition for a WhileStatementSyntax");

            Assert.That.AreEqual("retries > 0",
                                 doLoop.Condition?.SourceCode,
                                 because: "the condition of a do loop sits after its body",
                                 fix: "Set Condition for a DoStatementSyntax");

            Assert.That.AreEqual(StatementKind.Expression,
                                 labeled.Children.Single().Kind,
                                 because: "a label holds the statement it names",
                                 fix: "Build the statement of a LabeledStatementSyntax as its child");

            Assert.That.AreEqual("Use",
                                 fixedStatement.Invocations.Single().Name,
                                 because: "the calls inside a fixed block belong to it",
                                 fix: "Build the body of a FixedStatementSyntax as its child");
        }

        [TestMethod]
        public void Anonymous_Method_Parameters_Are_Reported()
        {
            var lambda = ParseMethod("Action<int> log = delegate (int code) { Write(code); };").Lambdas.Single();

            Assert.That.AreEqual("code",
                                 lambda.Parameters.Single().Name,
                                 because: "delegate (int code) { } has parameters like a lambda",
                                 fix: "Read the ParameterList of an AnonymousMethodExpressionSyntax");
        }

        [TestMethod]
        [DataRow("public partial void Go();", Modifier.Partial)]
        [DataRow("public required int X { get; set; }", Modifier.Required)]
        [DataRow("public static extern void Go();", Modifier.Extern)]
        [DataRow("public unsafe void Go() { }", Modifier.Unsafe)]
        [DataRow("private volatile int _x;", Modifier.Volatile)]
        [DataRow("private unsafe fixed int _buffer[4];", Modifier.Fixed)]
        public void Every_Member_Modifier_Is_Reported(string member, Modifier expected)
        {
            var holder = ParseCode($$"""
                public unsafe partial struct Holder
                {
                    {{member}}
                }
                """).Structs.Single();

            var modifiers = holder.Methods.Select(m => m.Modifiers)
                                  .Concat(holder.Properties.Select(p => p.Modifiers))
                                  .Concat(holder.Fields.Select(f => f.Modifiers))
                                  .Single();

            Assert.That.Contains(modifiers,
                                 expected,
                                 because: $"{member} carries {expected}",
                                 fix: "Map the keyword in SyntaxTokenListExtensions.ToModifier");
        }

        [TestMethod]
        public void File_Modifier_Is_Reported_On_A_Type()
        {
            var type = ParseCode("file class Helper { }").Classes.Single();

            Assert.That.Contains(type.Modifiers,
                                 Modifier.File,
                                 because: "a file local type is invisible outside its file, which an architecture rule may care about",
                                 fix: "Map the file keyword in SyntaxTokenListExtensions.ToModifier");
        }

        [TestMethod]
        public void Parameter_Modifiers_Are_Reported()
        {
            var parameters = ParseCode("""
                public ref struct Holder
                {
                    public void Go(ref int a, in int b, out int c, scoped ref int d) { c = 0; }
                }
                """).Structs.Single().Methods.Single().Parameters;

            Assert.That.AreEqual<Modifier>([Modifier.Ref, Modifier.In, Modifier.Out, Modifier.Scoped, Modifier.Ref],
                                           parameters.SelectMany(p => p.Modifiers),
                                           because: "ref, in, out and scoped change how a parameter is passed",
                                           fix: "Map the keywords in SyntaxTokenListExtensions.ToModifier");
        }

        [TestMethod]
        public void Accessibility_Defaults_Are_Reported()
        {
            var tree = ParseCode("""
                public class Holder
                {
                    protected void Go()
                    {
                        int Local() => 1;
                    }
                }
                """);

            var go = tree.Classes.Single().Methods.Single();

            Assert.That.AreEqual(Accessibility.Protected,
                                 go.Accessibility,
                                 because: "protected alone is protected, not protected internal",
                                 fix: "Check SyntaxTokenListExtensions.ToAccessibility");

            Assert.That.AreEqual(Accessibility.NotApplicable,
                                 go.LocalFunctions.Single().Accessibility,
                                 because: "a local function has no accessibility",
                                 fix: "Return NotApplicable for a LocalFunctionStatementSyntax");
        }

        [TestMethod]
        public void Unusual_Type_Syntax_Is_Taken_Apart()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    private global::System.Guid _id;
                    private Alias::Foo _foo;
                    public ref int Go(Span<int> values)
                    {
                        scoped Span<int> slice = values;
                        return ref values[0];
                    }
                }
                """).Classes.Single();

            var id = holder.Fields.Single(f => f.Name == "_id").TypeReference;
            var foo = holder.Fields.Single(f => f.Name == "_foo").TypeReference;
            var go = holder.Methods.Single();

            Assert.That.IsTrue(id.Name == "Guid" && id.Is("System.Guid"),
                               because: "global:: only says where to start looking, the type is still System.Guid",
                               fix: "Drop the global alias in TypeSyntaxExtensions.ToTypeReference");

            Assert.That.IsTrue(foo.Name == "Foo" && foo.Qualifier == "Alias",
                               because: "an extern alias is what is written in front of the name",
                               fix: "Take the alias of an AliasQualifiedNameSyntax as Qualifier");

            Assert.That.AreEqual("int",
                                 go.ReturnType.Name,
                                 because: "ref int returns an int by reference",
                                 fix: "Unwrap a RefTypeSyntax");

            Assert.That.AreEqual("Span",
                                 go.LocalVariables.Single().TypeReference.Name,
                                 because: "scoped Span<int> declares a Span",
                                 fix: "Unwrap a ScopedTypeSyntax");
        }

        /// <summary>
        /// The text of an element is cut out of the file on first use. Two elements must be equal
        /// whether that happened yet or not, or a rule that compares findings breaks at random.
        /// </summary>
        /// <remarks>
        /// A member access holds no list. An element that does, a statement with its children, is
        /// only equal to itself, because the lists compare by reference.
        /// </remarks>
        [TestMethod]
        public void Elements_Are_Equal_Whether_Their_Text_Was_Read_Or_Not()
        {
            const string Body = "Use(DateTime.Now);\nUse(DateTime.Today);";

            var first = ParseMethod(Body).MemberAccesses;
            var second = ParseMethod(Body).MemberAccesses;

            _ = first[0].SourceCode;

            Assert.That.IsTrue(first[0].Equals(second[0]) && first[0].GetHashCode() == second[0].GetHashCode(),
                               because: "the same element at the same place is equal, read or not",
                               fix: "Compare SourceCode in CodeElement.Equals and leave it out of GetHashCode");

            Assert.That.IsFalse(first[0].Equals(second[1]),
                                because: "an element elsewhere in the file is a different element",
                                fix: "Compare Location in CodeElement.Equals");

            var firstString = ParseMethod("Log(\"saved\");").StringLiterals.Single();
            var secondString = ParseMethod("Log(\"saved\");").StringLiterals.Single();

            _ = firstString.EnclosingStatement;

            Assert.That.IsTrue(firstString.Equals(secondString) && firstString.GetHashCode() == secondString.GetHashCode(),
                               because: "the enclosing statement is cut out on first use as well",
                               fix: "Compare EnclosingStatement by value in StringLiteral.Equals");
        }

        [TestMethod]
        public void A_File_Is_Parsed_From_Disk()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");

            try
            {
                File.WriteAllText(path, "namespace Sample; public class FromDisk { }");

                var tree = CSharpSyntaxTree.Parse(path);

                Assert.That.IsTrue(tree.Classes.Single().Name == "FromDisk" && tree.FileName == path,
                                   because: "parsing a path reads the file and keeps the path for every location",
                                   fix: "Check CSharpParser.Parse(FileInfo)");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Queries_Walk_Nested_Types()
        {
            var tree = ParseCode("""
                public abstract partial class Outer
                {
                    private int _count;
                    internal string Name { get; set; }

                    public class Inner
                    {
                        protected virtual void Go() { }
                        public override string ToString() => "";
                        private int _inner;
                        public int Size { get; }
                    }
                }
                """);

            var outer = tree.Classes.Single();
            var inner = outer.NestedClasses.Single();

            Assert.That.AreEqual(2, tree.AllFields().Count, because: "fields of nested types count", fix: "Check QueryExtensions.AllFields");
            Assert.That.AreEqual(2, tree.AllProperties().Count, because: "properties of nested types count", fix: "Check QueryExtensions.AllProperties");
            Assert.That.AreEqual(1, outer.DescendantTypes().Count, because: "the types nested in a type, without itself", fix: "Check QueryExtensions.DescendantTypes");
            Assert.That.AreEqual(2, outer.SelfAndDescendantTypes().Count, because: "a type and the types nested in it", fix: "Check QueryExtensions.SelfAndDescendantTypes");
            Assert.That.IsTrue(outer.IsAbstract() && outer.IsPartial(), because: "the predicates read the modifiers", fix: "Check QueryExtensions");
            Assert.That.IsTrue(outer.Fields.Single().IsPrivate() && outer.Properties.Single().IsInternal(), because: "the predicates read the accessibility", fix: "Check QueryExtensions");
            Assert.That.IsTrue(inner.Methods[0].IsVirtual() && inner.Methods[1].IsOverride(), because: "the predicates read the modifiers", fix: "Check QueryExtensions");
        }
    }
}
