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
                    case "partial":
                        yield return Modifier.Partial;
                        break;
                    case "required":
                        yield return Modifier.Required;
                        break;
                }
            }
        }

        internal static ImmutableList<Enum> ToEnums(this ImmutableList<EnumDeclarationSyntax> enumDeclarationSyntaxes,
                                                     string filePath)
        {
            return enumDeclarationSyntaxes.Select(item => item.ToEnum(filePath)).ToImmutableList();
        }

        internal static Enum ToEnum(this EnumDeclarationSyntax enumDeclarationSyntax,
                                    string filePath)
        {
            Throw.IfNull(enumDeclarationSyntax);

            var nameSpace = enumDeclarationSyntax.SyntaxTree.GetNamespace();
            var enumFields = enumDeclarationSyntax.Members.Select(m => new EnumField(m.Identifier.ValueText, m.SyntaxTree.ToString(), filePath)).ToImmutableList();
            var modifiers = enumDeclarationSyntax.ToModifiers().ToImmutableList();
            var name = enumDeclarationSyntax.Identifier.ValueText;
            var fullQualifiedName = $"{nameSpace.Name}.{name}";
            var syntaxTree = enumDeclarationSyntax.ToString();
            var attributes = enumDeclarationSyntax.AttributeLists.ToAttributes(filePath);

            return new Enum(nameSpace, name, modifiers,
                            enumFields, attributes, fullQualifiedName,
                            syntaxTree, filePath);
        }
    }
}
