using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ConstructorDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(
            this ConstructorDeclarationSyntax constructorDeclarationSyntax)
        {
            foreach (var syntaxToken in constructorDeclarationSyntax.Modifiers)
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
                }
            }
        }

        internal static Constructor ToConstructor(this ConstructorDeclarationSyntax constructorDeclarationSyntax)
        {
            Throw.IfNull(() => constructorDeclarationSyntax);

            var parameters = constructorDeclarationSyntax.ParameterList.ToParameters().ToList();
            var arguments = constructorDeclarationSyntax.Initializer?.ArgumentList.Arguments.Select(a => a.ToString())
                .ToList();
            var modifiers = constructorDeclarationSyntax.ToModifiers();

            return new Constructor(parameters, arguments ?? Enumerable.Empty<string>(), modifiers);
        }

        internal static IEnumerable<Constructor> ToConstructors(
            this IEnumerable<ConstructorDeclarationSyntax> constructorDeclarationSyntaxes)
        {
            Throw.IfNull(() => constructorDeclarationSyntaxes);

            return constructorDeclarationSyntaxes.Select(c => c.ToConstructor());
        }
    }
}
