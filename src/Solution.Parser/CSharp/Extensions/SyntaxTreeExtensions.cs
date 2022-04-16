using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;

namespace Solution.Parser.CSharp
{
    internal static class SyntaxTreeExtensions
    {
        internal static IEnumerable<T> AllOfType<T>(this SyntaxTree syntaxTree)
            where T : SyntaxNode
        {
            Throw.IfNull(() => syntaxTree);

            var result = syntaxTree.GetRoot().DescendantNodes().OfType<T>();
            return result;
        }
    }
}
