using Extensions.Pack;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EqualsClauseSyntaxExtensions
    {
        internal static string? ToInitializer(this EqualsValueClauseSyntax equalsValueClauseSyntax)
        {
            if (equalsValueClauseSyntax.IsNull())
            {
                return null;
            }

            return equalsValueClauseSyntax.Value.ToString();
        }
    }
}
