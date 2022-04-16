using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EqualsClauseSyntaxExtensions
    {
        internal static Initializer ToInitializer(this EqualsValueClauseSyntax equalsValueClauseSyntax)
        {
            if (equalsValueClauseSyntax == null)
            {
                return null;
            }

            return new Initializer(equalsValueClauseSyntax.Value.ToString());
        }
    }
}
