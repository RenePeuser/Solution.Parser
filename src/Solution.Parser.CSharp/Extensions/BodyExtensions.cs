using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Shared extraction of the executable part of a member, so a method, a local function, a
    /// constructor, an operator and a finalizer all report their body the same way.
    /// </summary>
    internal readonly record struct MemberBody(string Text,
                                               ImmutableList<string> Statements,
                                               ImmutableList<string> LineStatements,
                                               ImmutableList<LocalFunction> LocalFunctions,
                                               bool IsExpressionBodied,
                                               bool IsIterator);

    internal static class BodyExtensions
    {
        internal static MemberBody ToMemberBody(this BlockSyntax? block, ArrowExpressionClauseSyntax? expressionBody, string filePath)
        {
            if (block is not null)
            {
                var text = block.ToString();

                return new MemberBody(text,
                                      block.Statements.Select(s => s.ToString()).ToImmutableList(),
                                      ToLineStatements(text),
                                      block.ToLocalFunctions(filePath),
                                      IsExpressionBodied: false,
                                      IsIterator: block.ContainsYield());
            }

            if (expressionBody is not null)
            {
                var text = expressionBody.Expression.ToString();

                return new MemberBody(text,
                                      ImmutableList.Create(text),
                                      ToLineStatements(text),
                                      ImmutableList<LocalFunction>.Empty,
                                      IsExpressionBodied: true,
                                      IsIterator: false);
            }

            return new MemberBody(string.Empty,
                                  ImmutableList<string>.Empty,
                                  ImmutableList<string>.Empty,
                                  ImmutableList<LocalFunction>.Empty,
                                  IsExpressionBodied: false,
                                  IsIterator: false);
        }

        /// <summary>
        /// The local functions of the block, including those nested inside another local function.
        /// </summary>
        internal static ImmutableList<LocalFunction> ToLocalFunctions(this SyntaxNode node, string filePath)
        {
            return node.DescendantNodes()
                       .OfType<LocalFunctionStatementSyntax>()
                       .Select(l => l.ToLocalFunction(filePath))
                       .ToImmutableList();
        }

        /// <summary>
        /// A yield inside a nested local function or lambda belongs to that one, not to the member, so
        /// the search stops at those boundaries.
        /// </summary>
        private static bool ContainsYield(this SyntaxNode node)
        {
            return node.DescendantNodes(descendIntoChildren: child => child is not (LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax))
                       .Any(n => n.IsKind(SyntaxKind.YieldReturnStatement) || n.IsKind(SyntaxKind.YieldBreakStatement));
        }

        /// <summary>
        /// The source lines of the body without the enclosing braces and without blank lines. Split on
        /// any line ending rather than on the platform one, so a file with the other platform's line
        /// endings is read the same way.
        /// </summary>
        private static ImmutableList<string> ToLineStatements(string body)
        {
            var lines = body.SplitLines().ToList();

            if (lines.Count > 0 && lines[0].Trim() == "{")
            {
                lines.RemoveAt(0);
            }

            if (lines.Count > 0 && lines[^1].Trim() == "}")
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToImmutableList();
        }
    }
}
