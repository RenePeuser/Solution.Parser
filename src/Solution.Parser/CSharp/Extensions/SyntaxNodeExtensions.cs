using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;

namespace Solution.Parser.CSharp
{
    internal static class SyntaxNodeExtensions
    {
        internal static IEnumerable<T> AllOfType<T>(this SyntaxNode syntaxNode)
        {
            Throw.IfNull(() => syntaxNode);

            return syntaxNode.DescendantNodes().OfType<T>();
        }
    }
}
