using System;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Semantic.Test.SemanticHelper;

namespace Solution.Parser.CSharp.Semantic.Test
{
    /// <summary>
    /// The full mode on code given in memory: which method a call binds to and which parameter each
    /// argument fills.
    /// </summary>
    [TestClass]
    public class InvocationSemanticsTest
    {
        private const string ResolverFix = "Check InvocationResolver.Bind, the IArgumentOperation carries the parameter of every argument";

        private const string Client = """
            using System.Net.Http;
            using System.Threading.Tasks;

            public static class ClientExtensions
            {
                public static Task AssertPostAsync(this HttpClient client, string url, string body, bool writeResponse = false) => Task.CompletedTask;
            }
            """;

        [TestMethod]
        public void A_Positional_Argument_Is_Bound_To_Its_Parameter()
        {
            var code = WithSymbols(Client + """

                public class Tests
                {
                    private readonly HttpClient Client = new();

                    public async Task Post() => await Client.AssertPostAsync("url", "body", true);
                }
                """);

            var call = code.AllTrees.AllInvocations().Named("AssertPostAsync").Single();
            var writeResponse = call.Argument("writeResponse");

            Assert.That.IsTrue(call.IsResolved, because: "the extension method is declared right there", fix: ResolverFix);
            Assert.That.IsNotNull(writeResponse, because: "the method has a parameter of that name", fix: ResolverFix);
            Assert.That.AreEqual("true", writeResponse!.Expression, because: "the third argument fills writeResponse", fix: ResolverFix);
            Assert.That.IsTrue(writeResponse.Is(true), because: "the literal is a constant the compiler knows", fix: ResolverFix);
            Assert.That.IsFalse(writeResponse.IsNamed, because: "it was passed by position", fix: ResolverFix);
            Assert.That.AreEqual(2, writeResponse.Argument?.Ordinal, because: "the written argument is linked as well", fix: ResolverFix);
        }

        [TestMethod]
        public void The_Receiver_Of_An_Extension_Method_Fills_Its_This_Parameter()
        {
            var code = WithSymbols(Client + """

                public class Tests
                {
                    public Task Post(HttpClient client) => client.AssertPostAsync("url", "body");
                }
                """);

            var call = code.AllTrees.AllInvocations().Single();

            Assert.That.AreEqual<string>(["client", "url", "body", "writeResponse"],
                                         call.BoundArguments.Select(a => a.Parameter.Name),
                                         because: "the method is reported as declared, this parameter included",
                                         fix: ResolverFix);

            var receiver = call.BoundArguments[0];

            Assert.That.IsTrue(receiver.IsReceiver, because: "client is passed in front of the dot", fix: ResolverFix);
            Assert.That.AreEqual("client", receiver.Expression, because: "the receiver is what is passed", fix: ResolverFix);
            Assert.That.AreEqual("\"url\"", call.Argument("url")?.Expression, because: "the shift by the receiver must not move the others", fix: ResolverFix);
        }

        [TestMethod]
        public void A_Left_Out_Argument_Counts_With_Its_Default()
        {
            var code = WithSymbols(Client + """

                public class Tests
                {
                    public Task Post(HttpClient client) => client.AssertPostAsync("url", "body");
                }
                """);

            var writeResponse = code.AllTrees.AllInvocations().Single().Argument("writeResponse")!;

            Assert.That.IsFalse(writeResponse.IsExplicit, because: "the caller did not pass it", fix: ResolverFix);
            Assert.That.IsNull(writeResponse.Expression, because: "nothing was written", fix: ResolverFix);
            Assert.That.IsTrue(writeResponse.Is(false), because: "the default value applies, a rule must see it", fix: ResolverFix);
        }

        [TestMethod]
        public void A_Constant_Or_A_Named_Argument_Is_Bound_As_Well()
        {
            var code = WithSymbols(Client + """

                public class Tests
                {
                    private const bool Yes = true;

                    public Task ByConstant(HttpClient client) => client.AssertPostAsync("url", "body", Yes);

                    public Task ByName(HttpClient client) => client.AssertPostAsync(body: "body", writeResponse: true, url: "url");
                }
                """);

            var calls = code.AllTrees.AllInvocations();

            Assert.That.All(calls,
                            c => c.Argument("writeResponse")!.Is(true),
                            "passes writeResponse: true",
                            because: "a const and a named argument in any order mean the same to the compiler",
                            fix: ResolverFix);

            Assert.That.AreEqual("\"url\"",
                                 calls[1].Argument("url")?.Expression,
                                 because: "named arguments are bound by name, not by position",
                                 fix: ResolverFix);
        }

        [TestMethod]
        public void An_Overload_Is_Picked_By_The_Argument_Types()
        {
            var code = WithSymbols("""
                public class Overloads
                {
                    public void Go(int number) { }
                    public void Go(string text) { }
                    public void Call() { Go("text"); Go(1); }
                }
                """);

            var calls = code.AllTrees.AllInvocations().Named("Go");

            Assert.That.AreEqual<string>(["string", "int"],
                                         calls.Select(c => c.Method!.Parameters.Single().Type),
                                         because: "two calls of the same name bind to different overloads",
                                         fix: "Check that the resolved symbol, not the name, decides the method");
        }

        [TestMethod]
        public void A_Method_Of_The_Code_Base_Is_The_Record_The_Syntax_Model_Already_Has()
        {
            var code = WithSymbols("""
                public class Service
                {
                    public void Run() { }
                    public void Call() => Run();
                }
                """);

            var tree = code.AllTrees.Single();
            var declared = tree.AllMethods().Single(m => m.Name == "Run");
            var resolved = tree.AllInvocations().Single().Method;

            Assert.That.IsTrue(ReferenceEquals(declared, resolved),
                               because: "one vocabulary: a resolved method of the code base is the very record a rule already walks",
                               fix: "Check SymbolToModel.FindDeclared, it matches the declaration by span in the cached tree");
            Assert.That.IsTrue(resolved!.Origin.IsSource, because: "it is declared in the code base", fix: "Check SymbolToModel.OriginOf");
        }

        [TestMethod]
        public void A_Framework_Method_Has_Parameter_Names_From_Metadata()
        {
            var code = WithSymbols("""
                public class Program
                {
                    public void Main() => System.Console.WriteLine("hello");
                }
                """);

            var call = code.AllTrees.AllInvocations().Single();

            Assert.That.AreEqual("System.Console.WriteLine", call.Method?.FullQualifiedName, because: "the method is known from metadata", fix: ResolverFix);
            Assert.That.AreEqual("value", call.Argument("value")?.Parameter.Name, because: "parameter names live in the metadata", fix: ResolverFix);
            Assert.That.IsFalse(call.Method!.Origin.IsSource, because: "Console is not declared in the code base", fix: "Check SymbolToModel.OriginOf");
        }

        [TestMethod]
        public void Params_Arguments_Are_Collected_On_Their_Parameter()
        {
            var code = WithSymbols("""
                public class Log
                {
                    public void Write(string format, params object[] values) { }
                    public void Call() => Write("{0} {1}", 1, 2);
                }
                """);

            var values = code.AllTrees.AllInvocations().Single().Argument("values")!;

            Assert.That.AreEqual("1, 2", values.Expression, because: "every element passed for params belongs to the one parameter", fix: ResolverFix);
            Assert.That.IsTrue(values.IsExplicit, because: "elements were passed", fix: ResolverFix);
        }

        [TestMethod]
        public void An_Unknown_Method_Is_Unresolved_Not_An_Exception()
        {
            var code = WithSymbols("""
                public class Broken
                {
                    public void Call() => DoesNotExist(true);
                }
                """);

            var call = code.AllTrees.AllInvocations().Single();

            Assert.That.AreEqual(ResolutionStatus.Unresolved, call.Resolution, because: "nothing of that name exists", fix: ResolverFix);
            Assert.That.IsNull(call.Method, because: "there is no method to report", fix: ResolverFix);
            Assert.That.IsNull(call.Argument("anything"), because: "an unresolved call binds no argument", fix: ResolverFix);
        }

        [TestMethod]
        public void An_Ambiguous_Call_Reports_Its_Candidates()
        {
            var code = WithSymbols("""
                public class Overloads
                {
                    public void Go(string text) { }
                    public void Go(int[] numbers) { }
                    public void Call() => Go(null);
                }
                """);

            var call = code.AllTrees.AllInvocations().Single();

            Assert.That.AreEqual(ResolutionStatus.Ambiguous, call.Resolution, because: "null fits both overloads equally", fix: ResolverFix);
            Assert.That.HasCount(2, call.Candidates, because: "a rule can tell the user which overloads compete", fix: ResolverFix);
        }

        [TestMethod]
        public void The_Fast_Mode_Throws_Instead_Of_Guessing()
        {
            var code = SyntaxOnly("""
                public class Service
                {
                    public void Call() => Run(true);
                    public void Run(bool flag) { }
                }
                """);

            var call = code.AllTrees.AllInvocations().Single();

            var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _ = call.Method);

            Assert.That.IsTrue(exception.Message.Contains("WithSymbols", StringComparison.Ordinal),
                               because: "the message must tell how to get symbols",
                               fix: "Check SymbolsNotLoaded.Exception");
        }

        [TestMethod]
        public void The_Fast_Mode_Builds_No_Compilation()
        {
            var code = SyntaxOnly("public class A { void Go() => Go(); }");

            _ = code.AllTrees.AllInvocations();

            Assert.That.AreEqual(0, code.Workspace.CompilationCount,
                                 because: "symbols cost time and memory, a rule that does not ask for them must not pay",
                                 fix: "Check that nothing on the fast path calls Workspace.Compilation");

            var full = code.WithSymbols();
            _ = full.AllTrees.AllInvocations().Single().Method;
            _ = full.AllTrees.AllInvocations().Single().Method;

            Assert.That.AreEqual(1, code.Workspace.CompilationCount,
                                 because: "the full mode compiles once, on first use, and shares the caches with the fast mode",
                                 fix: "Check the Lazy in Workspace.Compilation");
        }
    }
}
