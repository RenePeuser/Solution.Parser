using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
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
                    case "const":
                        yield return Modifier.Const;
                        break;
                    case "abstract":
                        yield return Modifier.Abstract;
                        break;
                }
            }
        }

        internal static Property ToProperty(this PropertyDeclarationSyntax propertyDeclarationSyntax, string filePath)
        {
            Throw.IfNull(propertyDeclarationSyntax);

            var propertyType = propertyDeclarationSyntax.Type.ToString();
            var propertyName = propertyDeclarationSyntax.Identifier.Text;
            var isReadOnly = propertyDeclarationSyntax.ToString().DoesNotContain("set;");
            var modifiers = propertyDeclarationSyntax.ToModifiers().ToImmutableList();
            var syntaxTree = propertyDeclarationSyntax.ToString();
            var fullqualifiedName = BuildFullQualifiedName(propertyDeclarationSyntax);

            return new Property(propertyType, propertyName, isReadOnly, modifiers, syntaxTree, fullqualifiedName, filePath);
        }

        internal static string BuildFullQualifiedName(PropertyDeclarationSyntax recordDeclarationSyntax)
        {
            var getFullQualifiedName = GetFullQualifiedName().Reverse();

            var flattenParentNameSpaceQualifiers = getFullQualifiedName.Flatten(".");
            var fullQualifiedName = $"{flattenParentNameSpaceQualifiers}.{recordDeclarationSyntax.Identifier.ValueText}";
            return fullQualifiedName;

            IEnumerable<string> GetFullQualifiedName()
            {
                var parent = recordDeclarationSyntax.Parent;
                while (parent.IsNotNull())
                {
                    switch (parent)
                    {
                        case null:
                            break;
                        case ClassDeclarationSyntax classDeclarationSyntax:
                            parent = parent.Parent;
                            yield return classDeclarationSyntax.Identifier.ValueText;
                            break;
                        case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                            parent = parent.Parent;
                            yield return interfaceDeclarationSyntax.Identifier.ValueText;
                            break;
                        case RecordDeclarationSyntax recordDeclarationSyntax:
                            parent = parent.Parent;
                            yield return recordDeclarationSyntax.Identifier.ValueText;
                            break;
                        case NamespaceDeclarationSyntax namespaceDeclarationSyntax:
                            parent = null;
                            yield return namespaceDeclarationSyntax.ToNamespace().Name;
                            break;
                        default:
                            parent = null;
                            yield return string.Empty;
                            break;
                    }
                }
            }
        }

        internal static IImmutableList<Property> ToProperties(this IImmutableList<PropertyDeclarationSyntax> propertyDeclarationSyntaxes,
                                                              string filePath)
        {
            Throw.IfNull(propertyDeclarationSyntaxes);

            return propertyDeclarationSyntaxes.Select(p => p.ToProperty(filePath)).ToImmutableList();
        }
    }
}
