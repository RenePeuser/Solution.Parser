using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>The minimal API rules walked from MapGet up to the outermost call with Roslyn to find what is chained to it.</summary>
    [TestClass]
    public class InvocationChainTest
    {
        private static Method ParseMethod(string body)
        {
            return ParseCode($$"""
                public static class Endpoints
                {
                    public static void Map(IEndpointRouteBuilder app)
                    {
                {{body}}
                    }
                }
                """).Classes.Single().Methods.Single();
        }

        [TestMethod]
        public void Fluent_Calls_Form_One_Chain()
        {
            var method = ParseMethod("""
                        app.MapGet("/users", GetUsers)
                           .WithName("GetUsers")
                           .RequireAuthorization();
                """);

            var chain = method.InvocationChains.Single();

            Assert.That.AreEqual<string>(["MapGet", "WithName", "RequireAuthorization"],
                                         chain.Names,
                                         because: "a fluent registration reads as one chain, first call first",
                                         fix: "Follow Invocation.Receiver from the outermost call down");

            Assert.That.AreEqual("app",
                                 chain.Root,
                                 because: "the root is what the first call is made on",
                                 fix: "Take the target of the first call");

            Assert.That.AreEqual<string>(["WithName", "RequireAuthorization"],
                                         chain.After("MapGet").Select(c => c.Name),
                                         because: "a rule asks what is chained after the registration",
                                         fix: "Return the calls after the first one with the name");

            Assert.That.AreEqual("/users",
                                 chain.First.Arguments[0].StringValue,
                                 because: "the route of the endpoint is the first argument of the first call",
                                 fix: "Keep the classified arguments on each call of the chain");
        }

        [TestMethod]
        public void Receiver_Is_The_Same_Instance_As_In_The_Member()
        {
            var method = ParseMethod("""
                        var name = users.FirstOrDefault()?.GetName()!.Trim();
                """);

            var trim = method.Invocations.Named("Trim").Single();

            Assert.That.AreSame(method.Invocations.Named("GetName").Single(),
                                trim.Receiver!,
                                because: "a rule follows the chain by reference into the member's calls, through ?. and !",
                                fix: "Build the receiver first and share it through the cache in ToInvocations");

            Assert.That.AreEqual<string>(["FirstOrDefault", "GetName", "Trim"],
                                         trim.ChainUpToHere().Select(c => c.Name),
                                         because: "the chain up to a call starts at its first call",
                                         fix: "Walk Receiver until it is null and reverse");
        }

        /// <summary>
        /// In a?.B()?.C() the call .B() sits in the left part of the inner ?., so taking the nearest ?.
        /// made .B() its own receiver and overflowed the stack.
        /// </summary>
        [TestMethod]
        public void Chained_Conditional_Calls_Find_Their_Own_Receiver()
        {
            var method = ParseMethod("""
                        var value = source?.Load()?.Parse()?.Value();
                """);

            Assert.That.AreEqual<string>(["Load", "Parse", "Value"],
                                         method.Invocations.Named("Value").Single().ChainUpToHere().Select(c => c.Name),
                                         because: "each ?. call is made on the result of the one before it",
                                         fix: "Take the conditional access whose WhenNotNull holds the binding, not the nearest one");

            Assert.That.AreEqual<string>(["source", "source?.Load()", "source?.Load()?.Parse()"],
                                         method.Invocations.OrderBy(i => i.Location.SpanStart).Select(i => i.Target ?? string.Empty),
                                         because: "the target of a ?. call is what stands before its own ?.",
                                         fix: "Take the conditional access whose WhenNotNull holds the binding, not the nearest one");
        }

        [TestMethod]
        public void Every_Call_Is_In_Exactly_One_Chain()
        {
            var method = ParseMethod("""
                        app.MapGroup("/api").MapGet("/", Handle(Build()));
                        Log();
                """);

            Assert.That.AreEqual<string>(["MapGroup > MapGet", "Handle", "Build", "Log"],
                                         method.InvocationChains.Select(c => string.Join(" > ", c.Names)),
                                         because: "an argument call is a chain of its own, and a lone call is a chain of one",
                                         fix: "Start a chain at every call that no other call is made on");
        }
    }
}
