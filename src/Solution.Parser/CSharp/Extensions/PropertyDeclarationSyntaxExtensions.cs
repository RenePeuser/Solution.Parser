using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class PropertyDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(this PropertyDeclarationSyntax classDeclarationSyntax)
        {
            foreach (var syntaxToken in classDeclarationSyntax.Modifiers)
            {
                switch (syntaxToken.Text)
                {
                    case "public":
                        yield return Modifier.Public;
                        break;
                    case "internal":
                        yield return Modifier.Internal;
                        break;
                    case "protected":
                        yield return Modifier.Protected;
                        break;
                    case "private":
                        yield return Modifier.Private;
                        break;
                    case "static":
                        yield return Modifier.Static;
                        break;
                }
            }
        }

        internal static Property ToProperty(this PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            Throw.IfNull(propertyDeclarationSyntax);

            var propertyType = propertyDeclarationSyntax.Type.ToString();
            var propertyName = propertyDeclarationSyntax.Identifier.Text;
            var isReadOnly = propertyDeclarationSyntax.Modifiers.Count == 1;
            var modifiers = propertyDeclarationSyntax.ToModifiers().ToImmutableList();

            return new Property(propertyType, propertyName, isReadOnly, modifiers);
        }

        internal static IImmutableList<Property> ToProperties(
            this IImmutableList<PropertyDeclarationSyntax> propertyDeclarationSyntaxes)
        {
            Throw.IfNull(propertyDeclarationSyntaxes);

            return propertyDeclarationSyntaxes.Select(p => p.ToProperty()).ToImmutableList();
        }
    }
}
