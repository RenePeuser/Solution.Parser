using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    [TestClass]
    public class StatementTest
    {
        private static Method ParseMethod(string body)
        {
            return ParseCode($$"""
                public class Holder
                {
                    public async Task Go(object input)
                    {
                {{body}}
                    }
                }
                """).Classes.Single().Methods.Single();
        }

        /// <summary>The async rules searched the body for "try" as text.</summary>
        [TestMethod]
        public void Try_Catch_Is_Taken_Apart()
        {
            var method = ParseMethod("""
                        try
                        {
                            await SaveAsync();
                        }
                        catch (HttpRequestException ex) when (ex.StatusCode == 404)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            Cleanup();
                        }
                """);

            var tryStatement = method.TryStatements.Single();

            Assert.That.IsTrue(method.HasTryCatch && tryStatement.HasFinally,
                               because: "a rule about error handling starts from the try and its parts",
                               fix: "Build a TryStatement with its catches and its finally block");

            var (specific, all) = (tryStatement.Catches[0], tryStatement.Catches[1]);

            Assert.That.AreEqual("HttpRequestException",
                                 specific.ExceptionType.Name,
                                 because: "the caught type is what a rule about catching too much compares",
                                 fix: "Take the type of the CatchDeclarationSyntax");

            Assert.That.AreEqual("ex",
                                 specific.VariableName,
                                 because: "the name of the caught exception is part of the clause",
                                 fix: "Take the identifier of the CatchDeclarationSyntax");

            Assert.That.AreEqual("ex.StatusCode == 404",
                                 specific.Filter?.SourceCode,
                                 because: "an exception filter narrows what is caught",
                                 fix: "Take the expression of the CatchFilterClauseSyntax");

            Assert.That.IsTrue(specific.IsRethrowing && !specific.IsEmpty,
                               because: "throw; passes the exception on with its stack trace",
                               fix: "Look for a ThrowStatementSyntax without an expression in the catch block");

            Assert.That.IsTrue(all.CatchesAnyException && all.IsEmpty && !all.IsThrowing,
                               because: "an empty catch (Exception) swallows everything, the classic finding",
                               fix: "Set CatchesAnyException for Exception and IsEmpty for a block without statements");

            Assert.That.AreEqual<string>(["SaveAsync"],
                                         tryStatement.Block.Invocations.Select(i => i.Name),
                                         because: "a rule asks which calls are protected by the try",
                                         fix: "Cut the calls of a statement out of the member's calls by span");
        }

        [TestMethod]
        public void Switch_Statement_Is_Taken_Apart()
        {
            var method = ParseMethod("""
                        switch (input)
                        {
                            case "admin":
                            case "root":
                                Grant();
                                break;
                            case int count when count > 10:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(input));
                        }
                """);

            var switchStatement = method.SwitchStatements.Single();

            Assert.That.AreEqual("input",
                                 switchStatement.Subject.SourceCode,
                                 because: "what is switched on is part of the switch",
                                 fix: "Take the expression of the SwitchStatementSyntax");

            Assert.That.HasCount(3,
                                 switchStatement.Sections,
                                 because: "two labels sharing statements are one section",
                                 fix: "Build one SwitchSection per SwitchSectionSyntax");

            Assert.That.AreEqual<string>(["admin", "root"],
                                         switchStatement.Sections[0].Labels.Select(l => l.Value?.StringValue ?? string.Empty),
                                         because: "the values of the labels are what a rule compares",
                                         fix: "Classify the value of each CaseSwitchLabelSyntax");

            Assert.That.AreEqual("count > 10",
                                 switchStatement.Sections[1].Labels.Single().WhenClause?.SourceCode,
                                 because: "a pattern label can carry a condition",
                                 fix: "Take the condition of the WhenClauseSyntax");

            Assert.That.IsTrue(switchStatement.HasDefault,
                               because: "a rule may require a default section",
                               fix: "Set IsDefault for a DefaultSwitchLabelSyntax");

            Assert.That.AreEqual("ArgumentOutOfRangeException",
                                 method.Throws.Single().ExceptionType?.Name,
                                 because: "the thrown type is what a rule about exceptions compares",
                                 fix: "Take the type of the object creation that is thrown");
        }

        [TestMethod]
        public void Switch_Expression_Is_Taken_Apart()
        {
            var method = ParseMethod("""
                        var label = input switch
                        {
                            int n when n > 0 => "positive",
                            null => "none",
                            _ => "other"
                        };
                """);

            var switchExpression = method.SwitchExpressions.Single();

            Assert.That.AreEqual<string>(["int n", "null", "_"],
                                         switchExpression.Arms.Select(a => a.Pattern),
                                         because: "every arm is reported with its pattern",
                                         fix: "Take the pattern of each SwitchExpressionArmSyntax");

            Assert.That.AreEqual("positive",
                                 switchExpression.Arms[0].Result.StringValue,
                                 because: "the result of an arm is an expression like any other",
                                 fix: "Classify the expression of each arm");

            Assert.That.IsTrue(switchExpression.HasDiscardArm,
                               because: "a rule may require a catch all arm",
                               fix: "Set IsDiscard for a DiscardPatternSyntax without a when clause");
        }

        [TestMethod]
        public void Statements_Are_Nested_And_Flat()
        {
            var method = ParseMethod("""
                        if (input is null)
                        {
                            return;
                        }
                        else if (input is string)
                        {
                            foreach (var item in items) { await item.SaveAsync(); }
                        }
                        lock (_gate) { Count++; }
                """);

            Assert.That.AreEqual<StatementKind>([StatementKind.If, StatementKind.Lock],
                                                method.BodyStatements.Select(s => s.Kind),
                                                because: "the body holds its statements nested, an if with its branches",
                                                fix: "Build the statements of the body block only");

            var ifStatement = (IfStatement)method.BodyStatements[0];

            Assert.That.IsOfType<IfStatement>(ifStatement.Else,
                                              because: "an else if is an if in the else branch",
                                              fix: "Build the statement of the ElseClauseSyntax");

            Assert.That.AreEqual("input is null",
                                 ifStatement.Condition.SourceCode,
                                 because: "the condition is part of the if",
                                 fix: "Classify the condition of the IfStatementSyntax");

            var loop = method.Loops.Single();

            Assert.That.IsTrue(loop.Kind == StatementKind.ForEach && loop.Collection?.SourceCode == "items" && loop.Variables.Single().Name == "item",
                               because: "a foreach walks a collection with a variable",
                               fix: "Build a LoopStatement with collection and variables");

            Assert.That.HasCount(1,
                                 method.Returns,
                                 because: "AllStatements reaches statements at any depth",
                                 fix: "Collect every StatementSyntax of the body, not only the top level");

            Assert.That.AreEqual("item.SaveAsync()",
                                 method.Awaits.Single().Operand?.SourceCode,
                                 because: "what is awaited is the operand of the await",
                                 fix: "Classify every AwaitExpressionSyntax with its operand");

            Assert.That.AreSame(loop,
                                method.AllStatements.OfType<LoopStatement>().Single(),
                                because: "the nested and the flat view must hold the same statements",
                                fix: "Build each statement once and share it");
        }

        [TestMethod]
        public void Lambdas_Are_Reported_With_Their_Statements()
        {
            var method = ParseMethod("""
                        app.MapGet("/", static async (int id, CancellationToken token) =>
                        {
                            var user = await Load(id);
                            return user;
                        });
                        var ids = users.Select(u => u.Id);
                """);

            Assert.That.HasCount(2,
                                 method.Lambdas,
                                 because: "every lambda of the body is reported",
                                 fix: "Collect every AnonymousFunctionExpressionSyntax");

            var handler = method.Lambdas[0];

            Assert.That.IsTrue(handler.IsAsync && handler.IsStatic && !handler.IsExpressionBodied,
                               because: "a rule about endpoint handlers inspects the shape of the lambda",
                               fix: "Read the modifiers and the body kind of the lambda");

            Assert.That.AreEqual<string>(["int", "CancellationToken"],
                                         handler.Parameters.Select(p => p.Type),
                                         because: "the parameters of a handler are what the minimal API rules check",
                                         fix: "Map the parameter list of the lambda");

            Assert.That.AreEqual<StatementKind>([StatementKind.LocalDeclaration, StatementKind.Return],
                                                handler.Statements.Select(s => s.Kind),
                                                because: "a block bodied lambda has statements of its own",
                                                fix: "Build the statements of the lambda block");

            Assert.That.AreEqual("u",
                                 method.Lambdas[1].Parameters.Single().Name,
                                 because: "a simple lambda has one parameter without a type",
                                 fix: "Map the single parameter of a SimpleLambdaExpressionSyntax");
        }

        [TestMethod]
        public void Top_Level_Statements_Are_Typed()
        {
            var tree = ParseCode("""
                var builder = WebApplication.CreateBuilder(args);
                if (builder.Environment.IsDevelopment()) { builder.Services.AddDevTools(); }
                """);

            Assert.That.AreEqual<StatementKind>([StatementKind.LocalDeclaration, StatementKind.If],
                                                tree.Statements.Select(s => s.Kind),
                                                because: "top level statements are statements like those in a body",
                                                fix: "Build the global statements through the StatementBuilder");

            Assert.That.AreEqual<string>(["IsDevelopment", "AddDevTools"],
                                         tree.Statements[1].Invocations.Select(i => i.Name),
                                         because: "a rule about the program file asks which calls a statement makes",
                                         fix: "Collect the calls of the top level statements");
        }
    }
}
