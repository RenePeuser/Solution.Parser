using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class PropertyDeclarationSyntaxExtensions
    {
        internal static Property ToProperty(this PropertyDeclarationSyntax propertyDeclarationSyntax, string filePath)
        {
            Throw.IfNull(propertyDeclarationSyntax);

            var name = propertyDeclarationSyntax.Identifier.Text;

            return new Property
            {
                Name = name,
                FullyQualifiedName = propertyDeclarationSyntax.BuildFullyQualifiedName(name),
                Type = propertyDeclarationSyntax.Type.ToSourceCode(),
                Modifiers = propertyDeclarationSyntax.Modifiers.ToModifiers(),
                Accessibility = propertyDeclarationSyntax.Modifiers.ToAccessibility(propertyDeclarationSyntax),
                Attributes = propertyDeclarationSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = propertyDeclarationSyntax.ToDocumentation(),
                Accessors = propertyDeclarationSyntax.AccessorList.ToAccessors(propertyDeclarationSyntax.ExpressionBody, filePath),
                Initializer = propertyDeclarationSyntax.Initializer?.ToInitializer(),
                IsAutoProperty = propertyDeclarationSyntax.AccessorList.IsAutoImplemented(),
                IsExpressionBodied = propertyDeclarationSyntax.ExpressionBody is not null,
                ExplicitInterfaceSpecifier = propertyDeclarationSyntax.ExplicitInterfaceSpecifier?.Name.ToSourceCode(),
                IsNullable = propertyDeclarationSyntax.Type is NullableTypeSyntax,
                SourceCode = propertyDeclarationSyntax.ToSourceCode(),
                FilePath = filePath,
                Location = propertyDeclarationSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Property> ToProperties(this ImmutableList<PropertyDeclarationSyntax> propertyDeclarationSyntaxes,
                                                             string filePath)
        {
            Throw.IfNull(propertyDeclarationSyntaxes);

            return propertyDeclarationSyntaxes.Select(p => p.ToProperty(filePath)).ToImmutableList();
        }
    }
}
