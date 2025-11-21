using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;

namespace Solution.Parser.CSharp
{
    internal static class SyntaxNodeExtensions
    {
        internal static ImmutableList<T> AllOfType<T>(this SyntaxNode syntaxNode)
        {
            Throw.IfNull(syntaxNode);

            return syntaxNode.DescendantNodes().OfType<T>().ToImmutableList();
        }
    }
}
