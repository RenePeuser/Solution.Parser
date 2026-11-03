using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Walks a body once and sorts out the nodes every part of the body model is built from. Each
    /// list is in source order. Like the calls, what sits in a lambda belongs to the member, while a
    /// local function reports its own, so the walk does not descend into one.
    /// </summary>
    /// <remarks>
    /// The body model has a dozen parts. Walking the body once per part made building the model the
    /// most expensive thing the parser did, so they share this one walk.
    /// </remarks>
    internal sealed class BodyScan
    {
        internal List<InvocationExpressionSyntax> Invocations { get; } = [];

        /// <summary>Declarators, foreach statements, catch declarations and designations: whatever declares a local variable.</summary>
        internal List<SyntaxNode> Variables { get; } = [];

        internal List<BaseObjectCreationExpressionSyntax> ObjectCreations { get; } = [];

        /// <summary>String literals and interpolated strings.</summary>
        internal List<ExpressionSyntax> Strings { get; } = [];

        /// <summary>Simple member accesses and the member bindings of <c>?.</c>.</summary>
        internal List<ExpressionSyntax> MemberAccesses { get; } = [];

        /// <summary>Every statement below the root, the root itself excluded.</summary>
        internal List<StatementSyntax> Statements { get; } = [];

        internal List<SwitchExpressionSyntax> SwitchExpressions { get; } = [];

        internal List<AnonymousFunctionExpressionSyntax> Lambdas { get; } = [];

        internal List<AwaitExpressionSyntax> Awaits { get; } = [];

        /// <summary>The local functions, those nested in another local function included.</summary>
        internal List<LocalFunctionStatementSyntax> LocalFunctions { get; } = [];

        /// <summary>True when the member itself yields; a yield in a lambda or local function does not count.</summary>
        internal bool HasYield { get; private set; }

        internal static BodyScan Of(SyntaxNode root)
        {
            var scan = new BodyScan();

            foreach (var node in root.DescendantNodesAndSelf(descendIntoChildren: child => child == root || child is not LocalFunctionStatementSyntax))
            {
                scan.Add(node, root);
            }

            return scan;
        }

        /// <summary>
        /// The top level statements of a file, walked as one body and each statement itself included. A
        /// local function written at top level has no member of its own to report its code, so it counts
        /// here, as it always did for the calls; one nested in it reports its own, as in a member.
        /// </summary>
        internal static BodyScan OfTopLevel(IEnumerable<StatementSyntax> statements)
        {
            var scan = new BodyScan();

            foreach (var statement in statements)
            {
                foreach (var node in statement.DescendantNodesAndSelf(descendIntoChildren: child => child == statement || child is not LocalFunctionStatementSyntax))
                {
                    scan.Add(node, root: null);
                }
            }

            return scan;
        }

        private void Add(SyntaxNode node, SyntaxNode? root)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    Invocations.Add(invocation);
                    break;

                case VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax }:
                case CatchDeclarationSyntax:
                case SingleVariableDesignationSyntax:
                    Variables.Add(node);
                    break;

                case ForEachStatementSyntax forEach:
                    Variables.Add(forEach);
                    Statements.Add(forEach);
                    break;

                case LocalFunctionStatementSyntax localFunction:
                    LocalFunctions.Add(localFunction);
                    LocalFunctions.AddRange(localFunction.DescendantNodes().OfType<LocalFunctionStatementSyntax>());
                    Statements.Add(localFunction);
                    break;

                case YieldStatementSyntax yieldStatement:
                    HasYield |= !yieldStatement.Ancestors().TakeWhile(a => a != root).Any(a => a is AnonymousFunctionExpressionSyntax);
                    Statements.Add(yieldStatement);
                    break;

                case StatementSyntax statement when statement != root:
                    Statements.Add(statement);
                    break;

                case BaseObjectCreationExpressionSyntax creation:
                    ObjectCreations.Add(creation);
                    break;

                case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression):
                case InterpolatedStringExpressionSyntax:
                    Strings.Add((ExpressionSyntax)node);
                    break;

                case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                case MemberBindingExpressionSyntax:
                    MemberAccesses.Add((ExpressionSyntax)node);
                    break;

                case SwitchExpressionSyntax switchExpression:
                    SwitchExpressions.Add(switchExpression);
                    break;

                case AnonymousFunctionExpressionSyntax lambda:
                    Lambdas.Add(lambda);
                    break;

                case AwaitExpressionSyntax await:
                    Awaits.Add(await);
                    break;
            }
        }
    }
}
