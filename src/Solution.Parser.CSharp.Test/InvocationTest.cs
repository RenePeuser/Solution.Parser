using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// The method calls of a body as the syntax tells them, without symbols.
    /// </summary>
    [TestClass]
    public class InvocationTest
    {
        private const string InvocationFix = "Check InvocationExpressionSyntaxExtensions.ToInvocations and where BodyExtensions.ToMemberBody calls it";

        [TestMethod]
        public void A_Call_Reports_Name_Target_And_Arguments_In_Source_Order()
        {
            var method = ParseCode("""
                class Tests
                {
                    void Post() => Client.AssertPostAsync("url", "body", true);
                }
                """).Classes.Single().Methods.Single();

            var call = method.Invocations.Single();

            Assert.That.AreEqual("AssertPostAsync", call.Name, because: "the name is what a rule filters on first", fix: InvocationFix);
            Assert.That.AreEqual("Client", call.Target, because: "the receiver tells calls of the same name apart", fix: InvocationFix);
            Assert.That.AreEqual<string>(["\"url\"", "\"body\"", "true"],
                                         call.Arguments.Select(a => a.Expression),
                                         because: "arguments are reported as written, in source order",
                                         fix: InvocationFix);
            Assert.That.AreEqual<int>([0, 1, 2], call.Arguments.Select(a => a.Ordinal), because: "the ordinal is the position", fix: InvocationFix);
            Assert.That.All(call.Arguments, a => !a.IsNamed, "is positional", because: "no argument carries a name", fix: InvocationFix);
        }

        [TestMethod]
        public void A_Named_Argument_Is_Found_By_Its_Name()
        {
            var call = ParseCode("""
                class Tests
                {
                    void Post() { Client.AssertPostAsync("url", "body", writeResponse: true); }
                }
                """).AllInvocations().Single();

            Assert.That.AreEqual("true",
                                 call.NamedArgument("writeResponse")?.Expression,
                                 because: "a named argument names its parameter, so the syntax alone can answer this",
                                 fix: InvocationFix);

            Assert.That.IsNull(call.NamedArgument("body"),
                               because: "a positional argument has no name, binding it to a parameter needs symbols",
                               fix: InvocationFix);
        }

        [TestMethod]
        public void A_Chained_Call_Reports_Every_Link()
        {
            var calls = ParseCode("""
                class Tests
                {
                    async Task Post() { await Client.PostAsync("url").ConfigureAwait(false); }
                }
                """).AllInvocations();

            Assert.That.AreEqual<string>(["ConfigureAwait", "PostAsync"],
                                         calls.Select(c => c.Name),
                                         because: "both calls happen, the outer one comes first in a pre order walk",
                                         fix: InvocationFix);

            Assert.That.AreEqual("Client.PostAsync(\"url\")",
                                 calls.Named("ConfigureAwait").Single().Target,
                                 because: "the receiver of the outer call is the inner call",
                                 fix: InvocationFix);
        }

        [TestMethod]
        public void A_Conditional_Call_Knows_Its_Receiver()
        {
            var call = ParseCode("""
                class Tests
                {
                    void Go(Service? service) { service?.Run<int>(ref x, out var y); }
                }
                """).AllInvocations().Single();

            Assert.That.AreEqual("Run", call.Name, because: "the name must not carry the ?. or the type arguments", fix: InvocationFix);
            Assert.That.AreEqual("service", call.Target, because: "the receiver of ?. sits on the conditional access", fix: InvocationFix);
            Assert.That.IsTrue(call.IsConditional, because: "the call only happens when service is not null", fix: InvocationFix);
            Assert.That.AreEqual<string>(["int"], call.TypeArguments, because: "explicit type arguments are kept", fix: InvocationFix);
            Assert.That.AreEqual<ArgumentRefKind>([ArgumentRefKind.Ref, ArgumentRefKind.Out],
                                                  call.Arguments.Select(a => a.RefKind),
                                                  because: "ref and out change what a call can do to its arguments",
                                                  fix: InvocationFix);
            Assert.That.AreEqual<string>(["x", "var y"],
                                         call.Arguments.Select(a => a.Expression),
                                         because: "the expression is written without its ref kind keyword",
                                         fix: InvocationFix);
        }

        [TestMethod]
        public void A_Lambda_Belongs_To_The_Member_A_Local_Function_Does_Not()
        {
            var method = ParseCode("""
                class Tests
                {
                    void Go()
                    {
                        items.ForEach(i => Inside(i));
                        Local();

                        void Local() => OnlyLocal();
                    }
                }
                """).Classes.Single().Methods.Single();

            Assert.That.AreEqual<string>(["ForEach", "Inside", "Local"],
                                         method.Invocations.Select(i => i.Name),
                                         because: "a lambda has no model of its own, its calls happen on behalf of the member",
                                         fix: InvocationFix);

            Assert.That.AreEqual<string>(["OnlyLocal"],
                                         method.LocalFunctions.Single().Invocations.Select(i => i.Name),
                                         because: "a local function is reported on its own and owns its calls",
                                         fix: InvocationFix);
        }

        [TestMethod]
        public void All_Invocations_Covers_Constructors_And_Local_Functions()
        {
            var calls = ParseCode("""
                class Outer
                {
                    Outer() { Init(); }

                    class Inner
                    {
                        void Go() { Local(); void Local() => Deep(); }
                    }
                }
                """).AllInvocations();

            Assert.That.AreEqual<string>(["Init", "Local", "Deep"],
                                         calls.Select(c => c.Name),
                                         because: "a rule over all calls of a file must not miss constructors, nested types or local functions",
                                         fix: "Check QueryExtensions.AllMembersWithBody");
        }

        [TestMethod]
        public void A_Call_Points_At_Its_Source()
        {
            var call = ParseCode("""
                class Tests
                {
                    void Go()
                    {
                        Run(1);
                    }
                }
                """).AllInvocations().Single();

            Assert.That.AreEqual(5, call.Location.StartLine, because: "a finding must point at the call", fix: InvocationFix);
            Assert.That.AreEqual("Run(1)", call.SyntaxTree, because: "the source of the call is the call only", fix: InvocationFix);
        }
    }
}
