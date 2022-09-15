using System.Collections.Generic;
using System.Collections.Immutable;
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
                    case "static":
                        yield return Modifier.Static;
                        break;
                    case "const":
                        yield return Modifier.Const;
                        break;
                    case "abstract":
                        yield return Modifier.Abstract;
                        break;
                }
            }
        }

        internal static Constructor ToConstructor(this ConstructorDeclarationSyntax constructorDeclarationSyntax)
        {
            Throw.IfNull(constructorDeclarationSyntax);

            var parameters = constructorDeclarationSyntax.ParameterList.ToParameters().ToImmutableList();
            var arguments = constructorDeclarationSyntax.Initializer?.ArgumentList.Arguments.Select(a => a.ToString())
                .ToImmutableList();
            var modifiers = constructorDeclarationSyntax.ToModifiers().ToImmutableList();

            return new Constructor(parameters, arguments ?? ImmutableList<string>.Empty, modifiers);
        }

        internal static IImmutableList<Constructor> ToConstructors(this IImmutableList<ConstructorDeclarationSyntax> constructorDeclarationSyntaxes)
        {
            Throw.IfNull(constructorDeclarationSyntaxes);

            return constructorDeclarationSyntaxes.Select(c => c.ToConstructor()).ToImmutableList();
        }
    }
}
