using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EnumDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(this EnumDeclarationSyntax enumDeclarationSyntax)
        {
            foreach (var syntaxToken in enumDeclarationSyntax.Modifiers)
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

        internal static IImmutableList<Enum> ToEnums(this IImmutableList<EnumDeclarationSyntax> enumDeclarationSyntaxes)
        {
            return enumDeclarationSyntaxes.Select(ToEnum).ToImmutableList();
        }

        internal static Enum ToEnum(this EnumDeclarationSyntax enumDeclarationSyntax)
        {
            Throw.IfNull(enumDeclarationSyntax);

            var nameSpace = enumDeclarationSyntax.SyntaxTree.GetNamespace();
            var enumFields = enumDeclarationSyntax.Members.Select(m => new EnumField(m.Identifier.ValueText, m.SyntaxTree.ToString())).ToImmutableList();
            var modifiers = enumDeclarationSyntax.ToModifiers().ToImmutableList();
            var name = enumDeclarationSyntax.Identifier.ValueText;
            var fullQualifiedName = $"{nameSpace.Name}.{name}";
            var syntaxTree = enumDeclarationSyntax.ToString();

            return new Enum(nameSpace, name, modifiers, enumFields, fullQualifiedName, syntaxTree);
        }
    }
}
