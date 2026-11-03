using System.Collections.Immutable;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// A rule reads the code the way a developer does: the class, its method, the loop in it, the try
    /// in the loop. Every statement therefore answers the questions a method answers, only for the
    /// stretch of code it spans, so "a loop inside a loop" is two lines and no recursion.
    /// </summary>
    [TestClass]
    public class NestedStatementTest
    {
        private static Method Method(string body)
        {
            return ParseCode($$"""
                public class Holder
                {
                    public async Task Go(IEnumerable<Order> orders, int count)
                    {
                        {{body}}
                    }
                }
                """).Classes.Single().Methods.Single();
        }

        [TestMethod]
        public void A_Loop_Inside_A_Loop_Is_Found_From_The_Outer_One()
        {
            var method = Method("""
                foreach (var order in orders)
                {
                    for (var i = 0; i < count; i++)
                    {
                        while (true) { break; }
                    }
                }
                do { } while (false);
                """);

            var findings = from loop in method.Loops
                           from inner in loop.Loops
                           select $"{inner.Kind} in {loop.Kind}";

            Assert.That.AreEqual<string>(["For in ForEach", "While in ForEach", "While in For"],
                                         findings,
                                         because: "a loop holds every loop inside it at any depth, and only those",
                                         fix: "Cut Statement.AllStatements out of the body by the span of the statement");
        }

        [TestMethod]
        public void Every_Kind_Of_Loop_Has_A_Name_Of_Its_Own()
        {
            var method = Method("""
                for (var i = 0; i < count; i++) { }
                foreach (var order in orders) { }
                await foreach (var order in Stream()) { }
                while (count > 0) { count--; }
                do { count++; } while (count < 3);
                """);

            Assert.That.IsTrue(method.ForLoops.Count == 1 && method.ForEachLoops.Count == 2 &&
                               method.WhileLoops.Count == 1 && method.DoWhileLoops.Count == 1,
                               because: "for, foreach, while and do ... while are what a developer sees, so each has its list",
                               fix: "Check the loop lists in StatementViews");

            Assert.That.AreEqual<bool>([true, false, false, false, false],
                                       method.Loops.Select(l => l.IsFor),
                                       because: "only the first loop is a for",
                                       fix: "Check LoopStatement.IsFor");

            Assert.That.AreEqual<bool>([false, false, false, false, true],
                                       method.Loops.Select(l => l.IsDoWhile),
                                       because: "only the last loop is a do ... while",
                                       fix: "Check LoopStatement.IsDoWhile");

            Assert.That.IsTrue(method.Loops[1].IsForEach && method.Loops[2].IsForEach && method.Loops[2].IsAwait && method.Loops[3].IsWhile,
                               because: "await foreach is a foreach that awaits",
                               fix: "Check LoopStatement.IsForEach and IsWhile");
        }

        [TestMethod]
        public void A_Loop_Reports_What_Happens_In_It()
        {
            var method = Method("""
                foreach (var order in orders)
                {
                    var client = new HttpClient();
                    await client.SendAsync(order.ToRequest());
                    Log($"sent {order.Id} at {DateTime.Now}");
                    var total = order.Lines switch { [] => 0, _ => 1 };
                    orders.Where(o => o.Id > 0);
                }
                var after = new HttpClient();
                await Task.Delay(1);
                """);

            var loop = method.ForEachLoops.Single();

            Assert.That.AreEqual<string>(["HttpClient"],
                                         loop.ObjectCreations.Select(c => c.TypeName),
                                         because: "the new HttpClient() after the loop is not in it",
                                         fix: "Cut ObjectCreations out of the body by the span of the loop");

            Assert.That.AreEqual<string>(["client.SendAsync(order.ToRequest())"],
                                         loop.Awaits.Select(a => a.Operand!.SourceCode),
                                         because: "only the await inside the loop belongs to it",
                                         fix: "Cut Awaits out of the body by the span of the loop");

            Assert.That.AreEqual<string>(["order", "client", "total"],
                                         loop.LocalVariables.Select(v => v.Name),
                                         because: "the loop variable and the variables of its body are declared in the loop",
                                         fix: "Cut LocalVariables out of the body by the span of the loop");

            Assert.That.IsTrue(loop.MemberAccesses.Any(a => a.Target == "DateTime" && a.Name == "Now") &&
                               loop.StringLiterals.Single().IsInterpolated &&
                               loop.SwitchExpressions.Count == 1 &&
                               loop.Lambdas.Single().SourceCode == "o => o.Id > 0",
                               because: "a statement answers every question its member answers, for its own stretch of code",
                               fix: "Check StatementContents");
        }

        [TestMethod]
        public void A_Catch_That_Throws_Its_Exception_Again_Loses_The_Stack_Trace()
        {
            var method = Method("""
                try { await Save(); }
                catch (IOException ex) { throw ex; }
                catch (TimeoutException ex) { throw new SaveFailedException("timeout", ex); }
                catch (Exception) { throw; }
                """);

            var findings = from catchClause in method.CatchClauses
                           from throwStatement in catchClause.Block.Throws
                           where catchClause.VariableName is not null && throwStatement.Value?.Name == catchClause.VariableName
                           select throwStatement.SourceCode;

            Assert.That.AreEqual<string>(["throw ex;"],
                                         findings,
                                         because: "throw ex; restarts the stack trace, wrapping and throw; do not",
                                         fix: "Give the block of a catch its own Throws");
        }

        [TestMethod]
        public void What_A_Lambda_In_The_Statement_Does_Belongs_To_The_Statement()
        {
            var method = Method("""
                foreach (var order in orders)
                {
                    order.Lines.ForEach(line =>
                    {
                        if (line.Count == 0) { return; }
                    });
                }
                """);

            Assert.That.AreEqual<string>(["if (line.Count == 0) { return; }"],
                                         method.ForEachLoops.Single().IfStatements.Select(i => i.SourceCode),
                                         because: "the method counts the statements of its lambdas, so the loop that holds the lambda does too",
                                         fix: "Take the statements of a statement from the body, not from its Children");
        }

        [TestMethod]
        public void A_Local_Function_Keeps_Its_Statements_To_Itself()
        {
            var method = Method("""
                foreach (var order in orders)
                {
                    int Count() { while (true) { return 1; } }
                }
                """);

            var loop = method.ForEachLoops.Single();

            Assert.That.IsTrue(loop.WhileLoops.IsEmpty && loop.AllStatements.Any(s => s.Kind == StatementKind.LocalFunction),
                               because: "a local function reports its own body, as it does for the method",
                               fix: "Build the body index from the member's statements, which leave local functions out");

            Assert.That.AreEqual(1,
                                 method.LocalFunctions.Single().WhileLoops.Count,
                                 because: "the while belongs to the local function",
                                 fix: "Check that a local function gets a body index of its own");
        }

        [TestMethod]
        public void A_Statement_Asks_The_Same_As_Its_Member_For_Its_Own_Code()
        {
            var method = Method("""
                if (count > 0)
                {
                    try { using (var stream = Open()) { } }
                    catch (IOException) { }
                    switch (count) { case 1: return; }
                    throw new InvalidOperationException();
                }
                """);

            var ifStatement = method.IfStatements.Single();

            Assert.That.IsTrue(ifStatement.TryStatements.Count == 1 && ifStatement.CatchClauses.Count == 1 &&
                               ifStatement.UsingStatements.Count == 1 && ifStatement.SwitchStatements.Count == 1 &&
                               ifStatement.Returns.Count == 1 && ifStatement.Throws.Count == 1,
                               because: "everything here sits inside the if",
                               fix: "Check the statement lists in StatementViews");

            Assert.That.IsTrue(ifStatement.AllExpressions.Any(e => e.SourceCode == "count > 0") &&
                               ifStatement.AllExpressions.Any(e => e.SourceCode == "new InvalidOperationException()"),
                               because: "the expressions of a statement are its own and those of the statements inside it",
                               fix: "Check Statement.AllExpressions");

            Assert.That.AreEqual<string>(method.AllStatements.Skip(1).Select(s => s.SourceCode),
                                         ifStatement.AllStatements.Select(s => s.SourceCode),
                                         because: "the if holds every statement of the method but itself",
                                         fix: "Leave the statement itself out of its AllStatements");
        }

        [TestMethod]
        public void The_Lists_Are_Built_Once()
        {
            var method = Method("if (count > 0) { foreach (var order in orders) { } }");
            var ifStatement = method.IfStatements.Single();

            Assert.That.IsTrue(ReferenceEquals(method.IfStatements, method.IfStatements) &&
                               ReferenceEquals(method.AllExpressions, method.AllExpressions) &&
                               ReferenceEquals(ifStatement.ForEachLoops, ifStatement.ForEachLoops) &&
                               ReferenceEquals(ifStatement.LocalVariables, ifStatement.LocalVariables),
                               because: "a rule that walks thousands of members must not rebuild a list on every access",
                               fix: "Cache the views with Cache.For");
        }

        [TestMethod]
        public void A_Copy_Made_With_With_Computes_Its_Own_Lists()
        {
            var method = Method("if (count > 0) { }");

            _ = method.IfStatements;

            var copy = method with { AllStatements = ImmutableList<Statement>.Empty };

            Assert.That.IsTrue(copy.IfStatements.IsEmpty && method.IfStatements.Count == 1,
                               because: "with copies the cache along with the fields, so the cache must know whom it belongs to",
                               fix: "Check the owner in Cache.For");

            var ifStatement = method.IfStatements.Single();
            var handMade = ifStatement with { Children = ImmutableList<Statement>.Empty };

            Assert.That.IsTrue(handMade.AllStatements.IsEmpty,
                               because: "a copy with other children is not the statement the body was built with",
                               fix: "Fall back to the children when BodyIndex.StatementsInside does not find the instance");
        }

        [TestMethod]
        public void Reading_The_Lists_Does_Not_Change_Equality()
        {
            var first = Method("break;").AllStatements.Single();
            var second = Method("break;").AllStatements.Single();

            _ = first.AllStatements;
            _ = first.LocalVariables;

            Assert.That.IsTrue(first.Equals(second) && first.GetHashCode() == second.GetHashCode(),
                               because: "the same statement at the same place is equal, whether a rule read its lists yet or not",
                               fix: "Keep caches in Unequated fields, which records leave out of equality");
        }
    }
}
