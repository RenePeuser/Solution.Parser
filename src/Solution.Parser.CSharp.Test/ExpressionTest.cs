using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    [TestClass]
    public class ExpressionTest
    {
        private static Method ParseMethod(string body)
        {
            return ParseCode($$"""
                public class Holder
                {
                    public void Go()
                    {
                {{body}}
                    }
                }
                """).Classes.Single().Methods.Single();
        }

        /// <summary>The minimal API rules parsed arguments again with Roslyn to tell a literal from anything else.</summary>
        [TestMethod]
        public void Arguments_Are_Classified_With_Their_Value()
        {
            var call = ParseMethod("""
                        app.Map("api/users", typeof(User), nameof(User.Id), x => x.Id, 42, true, null!, $"v{version}/{{id}}");
                """).Invocations.Named("Map").Single();

            Assert.That.AreEqual<ExpressionKind>([ExpressionKind.StringLiteral,
                                                  ExpressionKind.TypeOf,
                                                  ExpressionKind.NameOf,
                                                  ExpressionKind.Lambda,
                                                  ExpressionKind.NumericLiteral,
                                                  ExpressionKind.BooleanLiteral,
                                                  ExpressionKind.NullLiteral,
                                                  ExpressionKind.InterpolatedString],
                                                 call.Arguments.Select(a => a.Kind),
                                                 because: "a rule asks what is passed, not how the text starts",
                                                 fix: "Classify the argument expression in ExpressionSyntaxExtensions.Classify");

            Assert.That.AreEqual("api/users",
                                 call.Arguments[0].StringValue,
                                 because: "the value of a string literal comes without quotes",
                                 fix: "Take the ValueText of the literal token");

            Assert.That.AreEqual("User",
                                 call.Arguments[1].ValueExpression?.TypeReference?.Name,
                                 because: "typeof names a type, which a rule compares as a type",
                                 fix: "Set TypeReference from the type of the TypeOfExpressionSyntax");

            Assert.That.AreEqual("Id",
                                 call.Arguments[2].StringValue,
                                 because: "nameof(User.Id) is the string Id",
                                 fix: "Take the last name of the nameof argument as LiteralValue");

            Assert.That.AreEqual(42,
                                 call.Arguments[4].LiteralValue,
                                 because: "a numeric literal has the value the compiler sees",
                                 fix: "Take the Value of the literal token");

            Assert.That.IsTrue(call.Arguments[6].ValueExpression!.HasNullForgiving,
                               because: "null! is a null literal with the warning suppressed, which a rule may want to ban",
                               fix: "Unwrap the SuppressNullableWarningExpression and flag it");

            Assert.That.AreEqual("v{}/{id}",
                                 call.Arguments[7].ValueExpression?.TextWithHoles(),
                                 because: "the text of a route template must read like the route, escaped braces turned into braces",
                                 fix: "Take the ValueText of each InterpolatedStringTextSyntax and a placeholder for each hole");
        }

        [TestMethod]
        public void Attribute_Arguments_Are_Classified()
        {
            var attribute = ParseCode("""
                [JsonConverter(typeof(MyConverter)), Route("api", Name = "users")]
                public class Holder { }
                """).Classes.Single().Attributes;

            Assert.That.AreEqual("MyConverter",
                                 attribute[0].PositionalExpressions.Single().TypeReference?.Name,
                                 because: "the converter type of an attribute was cut out of the text with IndexOf(\"typeof(\")",
                                 fix: "Classify every positional attribute argument");

            Assert.That.AreEqual("users",
                                 attribute[1].NamedArguments.Single().StringValue,
                                 because: "a named attribute argument is classified like any other",
                                 fix: "Set ValueExpression on each AttributeArgument");
        }

        [TestMethod]
        public void Initializers_Are_Classified()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    private string _name = string.Empty;
                    public List<int> Ids { get; set; } = [];
                    public void Go(int retries = 3) { }
                }
                """).Classes.Single();

            Assert.That.AreEqual(ExpressionKind.MemberAccess,
                                 holder.Fields.Single().InitializerExpression?.Kind,
                                 because: "string.Empty is a member access, not a literal",
                                 fix: "Set Field.InitializerExpression from the initializer value");

            Assert.That.AreEqual(ExpressionKind.CollectionExpression,
                                 holder.Properties.Single().InitializerExpression?.Kind,
                                 because: "[] is an empty collection expression",
                                 fix: "Set Property.InitializerExpression from the initializer value");

            Assert.That.AreEqual(3,
                                 holder.Methods.Single().Parameters.Single().DefaultValueExpression?.LiteralValue,
                                 because: "a default value is classified like any other expression",
                                 fix: "Set Parameter.DefaultValueExpression from the default value");
        }

        /// <summary>The service rules matched "new (?&lt;type&gt;\w+)" against the source.</summary>
        [TestMethod]
        public void Object_Creations_Are_Reported_Target_Typed_Ones_Included()
        {
            var creations = ParseMethod("""
                        var client = new HttpClient(handler) { Timeout = timeout };
                        List<int> ids = new();
                        Use(new());
                """).ObjectCreations;

            Assert.That.AreEqual<string>(["HttpClient", "List", ""],
                                         creations.Select(c => c.TypeName),
                                         because: "a target typed new() takes its type from the declaration, where the syntax tells it",
                                         fix: "Read the type of the declaration a new() initialises");

            Assert.That.AreEqual("handler",
                                 creations[0].Arguments.Single().Expression,
                                 because: "the constructor arguments are arguments like those of a call",
                                 fix: "Map the ArgumentList through ToInvocationArgument");

            Assert.That.AreEqual("Timeout",
                                 creations[0].InitializedMembers.Single().Name,
                                 because: "what an object initializer sets is part of the creation",
                                 fix: "Read the assignments of the ObjectInitializerExpression");

            Assert.That.IsTrue(creations[1].IsTargetTyped && creations[2].IsTargetTyped,
                               because: "a rule may want to tell new() apart",
                               fix: "Set IsTargetTyped for an ImplicitObjectCreationExpressionSyntax");
        }

        /// <summary>The SQL rule parsed every class again to collect its string literals.</summary>
        [TestMethod]
        public void String_Literals_Are_Reported_With_Their_Value()
        {
            var literals = ParseMethod(""""
                        var sql = $"SELECT * FROM {table}";
                        var path = @"C:\temp";
                        var raw = """
                            raw text
                            """;
                """").StringLiterals;

            Assert.That.AreEqual<string>(["SELECT * FROM {table}", @"C:\temp", "raw text"],
                                         literals.Select(l => l.Value),
                                         because: "the value is what a rule searches, without quotes and escapes",
                                         fix: "Take the ValueText of the token, the parts for an interpolated string");

            Assert.That.IsTrue(literals[0].IsInterpolated && literals[1].IsVerbatim && literals[2].IsRaw,
                               because: "a rule about SQL injection has to tell an interpolated string apart",
                               fix: "Derive the flags from the start token");

            Assert.That.AreEqual("var sql = $\"SELECT * FROM {table}\";",
                                 literals[0].EnclosingStatement,
                                 because: "a finding quotes the statement the string sits in",
                                 fix: "Take the first StatementSyntax ancestor");
        }

        /// <summary>The sample value rule searched member accesses with Roslyn and a regular expression.</summary>
        [TestMethod]
        public void Member_Accesses_Are_Reported()
        {
            var accesses = ParseMethod("""
                        var now = DateTime.Now;
                        var id = Guid.NewGuid();
                        var name = user?.Name;
                """).MemberAccesses;

            Assert.That.AreEqual<string>(["DateTime.Now", "Guid.NewGuid", "user.Name"],
                                         accesses.Select(a => $"{a.Target}.{a.Name}"),
                                         because: "target and name are what a rule about DateTime.Now compares",
                                         fix: "Split each MemberAccessExpressionSyntax into its expression and its name");

            Assert.That.IsTrue(accesses[1].IsInvoked && !accesses[0].IsInvoked,
                               because: "a rule tells a property read from a method call",
                               fix: "Set IsInvoked when the access is the expression of an invocation");

            Assert.That.IsTrue(accesses[2].IsConditional,
                               because: "user?.Name is an access through ?.",
                               fix: "Report a MemberBindingExpressionSyntax as a conditional access");
        }
    }
}
