using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class FieldDeclarationSyntaxExtensions
    {
        internal static Field ToField(this FieldDeclarationSyntax fieldDeclarationSyntax, string filePath)
        {
            Throw.IfNull(fieldDeclarationSyntax);

            var type = fieldDeclarationSyntax.Declaration.Type.ToString();
            var name = fieldDeclarationSyntax.Declaration.Variables[0].Identifier.Text;
            var bindingFlags = fieldDeclarationSyntax.ToBindingFlags().ToImmutableList();
            var initializer = fieldDeclarationSyntax.Declaration.Variables[0].Initializer?.ToInitializer();

            return new Field(name, type, bindingFlags, initializer, fieldDeclarationSyntax.SyntaxTree.ToString(), filePath);
        }

        public static ImmutableList<Field> ToFields(this ImmutableList<FieldDeclarationSyntax> fieldDeclarationSyntaxes, string filePath)
        {
            return fieldDeclarationSyntaxes.Select(f => f.ToField(filePath)).ToImmutableList();
        }

        private static IEnumerable<Modifier> ToBindingFlags(this FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            foreach (var syntaxToken in fieldDeclarationSyntax.Modifiers)
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
                    case "readonly":
                        yield return Modifier.ReadOnly;
                        break;
                    case "const":
                        yield return Modifier.Const;
                        break;
                    case "required":
                        yield return Modifier.Required;
                        break;
                }
            }
        }
    }
}
