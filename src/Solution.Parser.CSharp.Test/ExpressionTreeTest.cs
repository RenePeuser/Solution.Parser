using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// An expression used to tell its kind and nothing about its parts, so a rule about <c>x == null</c>
    /// or an assignment to a parameter had to read the text after all.
    /// </summary>
    [TestClass]
    public class ExpressionTreeTest
    {
        private static Method ParseMethod(string body)
        {
            return ParseCode($$"""
                public class Holder
                {
                    public void Go(Order order, int count)
                    {
                {{body}}
                    }
                }
                """).Classes.Single().Methods.Single();
        }

        private static Expression ParseInitializer(string expression)
        {
            return ParseMethod($"var r = {expression};").LocalVariables.Single(v => v.Name == "r").InitializerExpression!;
        }

        [TestMethod]
        public void A_Binary_Expression_Has_Its_Sides_And_Operator()
        {
            var comparison = ParseInitializer("order.Customer == null");

            Assert.That.IsTrue(comparison.Operator == "==" &&
                               comparison.Left is { Kind: ExpressionKind.MemberAccess, Name: "Customer" } &&
                               comparison.Right?.Kind == ExpressionKind.NullLiteral,
                               because: "x == null is the left side, the operator and the right side",
                               fix: "Set Left, Right and Operator for a BinaryExpressionSyntax");

            Assert.That.AreEqual("order",
                                 comparison.Left!.Target?.Name,
                                 because: "a member access knows what it is accessed on",
                                 fix: "Set Target for a MemberAccessExpressionSyntax");
        }

        [TestMethod]
        public void An_Assignment_Has_Target_Value_And_Operator()
        {
            var assignment = ((ExpressionStatement)ParseMethod("count += Compute(order);").AllStatements.Single()).Expression;

            Assert.That.IsTrue(assignment.Operator == "+=" && assignment.Left?.Name == "count" && assignment.Right?.Kind == ExpressionKind.Invocation,
                               because: "a rule against assigning to a parameter reads the left side",
                               fix: "Set Left, Right and Operator for an AssignmentExpressionSyntax");
        }

        [TestMethod]
        public void A_Conditional_And_A_Unary_Expression_Are_Taken_Apart()
        {
            var conditional = ParseInitializer("count > 0 ? count : -count");
            var postfix = ParseInitializer("count++");

            Assert.That.IsTrue(conditional.Condition?.Operator == ">" && conditional.WhenTrue?.Name == "count" && conditional.WhenFalse is { Operator: "-", IsPostfix: false },
                               because: "a ? b : c has a condition and two values",
                               fix: "Set Condition, WhenTrue and WhenFalse for a ConditionalExpressionSyntax");

            Assert.That.IsTrue(postfix is { Operator: "++", IsPostfix: true, Operand.Name: "count" },
                               because: "x++ and ++x differ in when the value is read",
                               fix: "Set Operator and IsPostfix for a PostfixUnaryExpressionSyntax");
        }

        [TestMethod]
        public void A_Call_Has_Name_Target_And_Arguments()
        {
            var call = ParseInitializer("client.Orders.SendAsync(order, count + 1)");

            Assert.That.IsTrue(call is { Kind: ExpressionKind.Invocation, Name: "SendAsync", Target: { Kind: ExpressionKind.MemberAccess, Name: "Orders" } },
                               because: "a call is a method name on a receiver",
                               fix: "Take Name and Target of a call from what is called");

            Assert.That.AreEqual<ExpressionKind>([ExpressionKind.Identifier, ExpressionKind.Binary],
                                                 call.Arguments.Select(a => a.Kind),
                                                 because: "the arguments are expressions as well",
                                                 fix: "Set Arguments for an InvocationExpressionSyntax");
        }

        [TestMethod]
        public void A_Conditional_Access_Finds_Its_Target()
        {
            var access = ParseInitializer("order?.Customer?.Name");
            var call = ParseInitializer("order?.Lines[0].Save()");

            Assert.That.IsTrue(access is { Name: "Name", IsConditional: true, Target: { Name: "Customer", IsConditional: true, Target.Name: "order" } },
                               because: "in a?.b?.c the target of c is a?.b, and that of b is a",
                               fix: "Resolve the target of a MemberBindingExpressionSyntax in ConditionalTarget");

            Assert.That.IsTrue(call is { Kind: ExpressionKind.Invocation, Name: "Save", Target: { Kind: ExpressionKind.ElementAccess, Target.Name: "Lines" } },
                               because: "a call after ?. is a call on what follows the ?.",
                               fix: "Unwrap a ConditionalAccessExpressionSyntax in ToExpression");
        }

        [TestMethod]
        public void Creations_And_Collections_Have_Elements()
        {
            var creation = ParseInitializer("new Order(id) { Total = 3, Note = \"x\" }");
            var collection = ParseInitializer("[1, ..others]");
            var tuple = ParseInitializer("(order, count)");
            var range = ParseInitializer("items[1..^1]");

            Assert.That.IsTrue(creation.Arguments.Single().Name == "id" &&
                               creation.Elements.Select(e => e.Left?.Name).SequenceEqual(["Total", "Note"]),
                               because: "new Foo(...) { ... } has arguments and an initializer",
                               fix: "Set Arguments and Elements for an ObjectCreationExpressionSyntax");

            Assert.That.AreEqual<ExpressionKind>([ExpressionKind.NumericLiteral, ExpressionKind.Spread],
                                                 collection.Elements.Select(e => e.Kind),
                                                 because: "a spread in a collection expression is an element of its own",
                                                 fix: "Map a SpreadElementSyntax to ExpressionKind.Spread");

            Assert.That.AreEqual(2, tuple.Elements.Count, because: "a tuple has its elements", fix: "Set Elements for a TupleExpressionSyntax");

            var slice = range.Arguments.Single();

            Assert.That.IsTrue(slice is { Kind: ExpressionKind.Range, Left.Kind: ExpressionKind.NumericLiteral, Right: { Kind: ExpressionKind.Unary, Operator: "^" } },
                               because: "1..^1 is a range from the second to the one before last",
                               fix: "Map a RangeExpressionSyntax to ExpressionKind.Range");
        }

        [TestMethod]
        public void Descendants_Walk_The_Whole_Tree()
        {
            var method = ParseMethod("""
                        if (order.Customer != null && Check(order.Lines.Count == 0))
                        {
                            Log($"empty {order.Id}");
                        }
                        var names = order.Lines.Select(l => l.Name == null ? "" : l.Name);
                """);

            var nullComparisons = method.AllExpressions
                                        .Where(e => e.Kind == ExpressionKind.Binary && e.Operator is "==" or "!=" && e.Right?.Kind == ExpressionKind.NullLiteral)
                                        .Select(e => e.SourceCode);

            Assert.That.AreEqual<string>(["order.Customer != null", "l.Name == null"],
                                         nullComparisons,
                                         because: "a rule finds every comparison with null, also inside a lambda",
                                         fix: "Walk Children in Expression.DescendantsAndSelf and gather the roots in MemberWithBody.AllExpressions");

            Assert.That.IsTrue(method.AllExpressions.Any(e => e.Kind == ExpressionKind.MemberAccess && e.Name == "Id"),
                               because: "the holes of an interpolated string are expressions inside it",
                               fix: "Include the holes in Expression.Children");
        }

        [TestMethod]
        public void An_Expression_Body_Is_Walked()
        {
            var method = ParseCode("""
                public class Holder
                {
                    public bool IsEmpty(Order order) => order.Lines == null || order.Lines.Count == 0;
                }
                """).Classes.Single().Methods.Single();

            Assert.That.IsTrue(method.ExpressionBody is { Kind: ExpressionKind.Binary, Operator: "||" } &&
                               method.AllExpressions.Count(e => e.Operator == "==") == 2,
                               because: "an expression bodied member has no statements, its expression is the body",
                               fix: "Set MemberWithBody.ExpressionBody and start AllExpressions from it");
        }

        /// <summary>The same call is both inside a statement and on the member's list of awaits; it is one instance.</summary>
        [TestMethod]
        public void A_Node_Is_Built_Once()
        {
            var method = ParseMethod("await SaveAsync(order);");
            var statement = (ExpressionStatement)method.AllStatements.Single();

            Assert.That.IsTrue(ReferenceEquals(statement.Expression, method.Awaits.Single()),
                               because: "building the same node twice costs time and memory for nothing",
                               fix: "Share expressions by node while a file is parsed, see ExpressionSyntaxExtensions.ShareExpressions");
        }

        [TestMethod]
        public void An_Is_Expression_Has_Its_Pattern()
        {
            var isNull = ParseInitializer("order is null");
            var isNotNull = ParseInitializer("order is not null");
            var empty = ParseInitializer("order is { } o");
            var oldStyle = ParseInitializer("order is SpecialOrder");

            Assert.That.IsTrue(isNull.Pattern!.IsNull && isNull.Operand?.Name == "order",
                               because: "x is null tests x against the null constant",
                               fix: "Set Operand and Pattern for an IsPatternExpressionSyntax");

            Assert.That.IsTrue(isNotNull.Pattern!.IsNotNull && empty.Pattern!.IsNotNull && empty.Pattern.Designation == "o",
                               because: "not null and { } both reject null only",
                               fix: "Check Pattern.IsNotNull");

            Assert.That.IsTrue(oldStyle.Pattern is { Kind: PatternKind.Type, TypeReference.Name: "SpecialOrder" },
                               because: "x is Foo is a type pattern, written the old way",
                               fix: "Build a type pattern for the IsExpression form of BinaryExpressionSyntax");

            var nested = ParseMethod("if (order is { Lines: [_, ..], Total: > Limit or < -Limit }) { }");

            Assert.That.AreEqual(2,
                                 nested.AllExpressions.Count(e => e.Name == "Limit"),
                                 because: "the values compared in a pattern are expressions of the body as well",
                                 fix: "Include the values of every nested pattern in Expression.Children");
        }

        [TestMethod]
        public void A_Recursive_Pattern_Is_Taken_Apart()
        {
            var pattern = ParseInitializer("order is SpecialOrder { Status: \"open\", Customer.Age: >= 18 and < 65 } special").Pattern!;

            Assert.That.IsTrue(pattern is { Kind: PatternKind.Recursive, TypeReference.Name: "SpecialOrder", Designation: "special" },
                               because: "a property pattern names a type and may declare a variable",
                               fix: "Read Type and Designation of a RecursivePatternSyntax");

            Assert.That.AreEqual<string>(["Status", "Customer.Age"],
                                         pattern.Properties.Select(p => p.Name),
                                         because: "each property pattern names its property, extended ones with the dot",
                                         fix: "Take the ExpressionColon of each subpattern");

            var age = pattern.Properties[1].Pattern;

            Assert.That.IsTrue(age is { Kind: PatternKind.And, Left: { Kind: PatternKind.Relational, Operator: ">=" }, Right.Value.LiteralValue: 65 },
                               because: ">= 18 and < 65 is an and of two relational patterns",
                               fix: "Map BinaryPatternSyntax and RelationalPatternSyntax");

            Assert.That.AreEqual("open",
                                 pattern.Properties[0].Pattern.Value?.StringValue,
                                 because: "a constant pattern holds its value",
                                 fix: "Set Value for a ConstantPatternSyntax");
        }

        [TestMethod]
        public void List_Positional_And_Var_Patterns_Are_Taken_Apart()
        {
            var list = ParseInitializer("items is [1, .., var last]").Pattern!;
            var positional = ParseInitializer("point is (0, _)").Pattern!;

            Assert.That.AreEqual<PatternKind>([PatternKind.Constant, PatternKind.Slice, PatternKind.Var],
                                              list.Elements.Select(e => e.Kind),
                                              because: "a list pattern has its elements, a slice among them",
                                              fix: "Map ListPatternSyntax, SlicePatternSyntax and VarPatternSyntax");

            Assert.That.AreEqual("last", list.Elements[2].Designation, because: "var last declares last", fix: "Read the designation of a VarPatternSyntax");

            Assert.That.AreEqual<PatternKind>([PatternKind.Constant, PatternKind.Discard],
                                              positional.Positional.Select(p => p.Kind),
                                              because: "(0, _) deconstructs and tests each part",
                                              fix: "Read the PositionalPatternClause of a RecursivePatternSyntax");
        }

        [TestMethod]
        public void Switch_Labels_And_Arms_Have_Typed_Patterns()
        {
            var method = ParseMethod("""
                        switch (order.Status)
                        {
                            case "open":
                            case Status.Closed or Status.Archived:
                                break;
                            case null:
                                break;
                        }
                        var label = order switch { SpecialOrder { Total: > 100 } => "big", null => "none", _ => "other" };
                """);

            var labels = method.SwitchStatements.Single().Labels;

            Assert.That.AreEqual<PatternKind>([PatternKind.Constant, PatternKind.Or, PatternKind.Constant],
                                              labels.Select(l => l.TypedPattern!.Kind),
                                              because: "case \"open\": is a constant pattern even without the pattern syntax",
                                              fix: "Set SwitchLabel.TypedPattern in StatementBuilder.ToLabel");

            Assert.That.IsTrue(labels[2].TypedPattern!.IsNull && labels[0].Value?.StringValue == "open",
                               because: "case null: is a null check and a constant keeps its value",
                               fix: "Set SwitchLabel.TypedPattern and Value");

            var arms = method.SwitchExpressions.Single().Arms;

            Assert.That.AreEqual<PatternKind>([PatternKind.Recursive, PatternKind.Constant, PatternKind.Discard],
                                              arms.Select(a => a.TypedPattern.Kind),
                                              because: "each arm of a switch expression has its pattern",
                                              fix: "Set SwitchExpressionArm.TypedPattern in StatementBuilder.BuildSwitchExpression");
        }

        [TestMethod]
        public void Statements_Hand_Out_Their_Expressions()
        {
            var method = ParseMethod("""
                        foreach (var line in order.Lines) { }
                        using (Open(order)) { }
                        lock (_gate) { }
                        throw Fail(count);
                """);

            Assert.That.AreEqual<string>(["order.Lines", "Open(order)", "_gate", "Fail(count)"],
                                         method.AllStatements.SelectMany(s => s.Expressions).Select(e => e.SourceCode),
                                         because: "a rule walks the expressions of every statement without knowing each statement type",
                                         fix: "Check Statement.Expressions");
        }
    }
}
