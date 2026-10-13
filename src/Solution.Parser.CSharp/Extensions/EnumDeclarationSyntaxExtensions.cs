using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class EnumDeclarationSyntaxExtensions
    {
        internal static Enum ToEnum(this EnumDeclarationSyntax enumDeclarationSyntax, string filePath)
        {
            Throw.IfNull(enumDeclarationSyntax);

            var name = enumDeclarationSyntax.Identifier.ValueText;
            var baseTypes = enumDeclarationSyntax.BaseList?.ToBaseTypes() ?? ImmutableList<BaseType>.Empty;

            return new Enum
            {
                Name = name,
                FullyQualifiedName = enumDeclarationSyntax.BuildFullyQualifiedName(name),
                Namespace = enumDeclarationSyntax.NamespaceOf(),
                Modifiers = enumDeclarationSyntax.Modifiers.ToModifiers(),
                Accessibility = enumDeclarationSyntax.Modifiers.ToAccessibility(enumDeclarationSyntax),
                Attributes = enumDeclarationSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = enumDeclarationSyntax.ToDocumentation(),
                BaseTypes = baseTypes,

                // An enum has at most one base type, and it is the underlying integral type.
                UnderlyingType = baseTypes.FirstOrDefault()?.TypeName,
                EnumFields = enumDeclarationSyntax.Members.Select(m => m.ToEnumField(filePath)).ToImmutableList(),
                SourceCode = enumDeclarationSyntax.ToSourceCode(),
                FilePath = filePath,
                Location = enumDeclarationSyntax.ToCodeLocation(filePath)
            };
        }

        private static EnumField ToEnumField(this EnumMemberDeclarationSyntax member, string filePath)
        {
            var name = member.Identifier.ValueText;

            return new EnumField
            {
                Name = name,
                FullyQualifiedName = member.BuildFullyQualifiedName(name),
                Attributes = member.AttributeLists.ToAttributes(filePath),
                Documentation = member.ToDocumentation(),
                Value = member.EqualsValue?.Value.ToSourceCode(),
                SourceCode = member.ToSourceCode(),
                FilePath = filePath,
                Location = member.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Enum> ToEnums(this ImmutableList<EnumDeclarationSyntax> enumDeclarationSyntaxes, string filePath)
        {
            return enumDeclarationSyntaxes.Select(item => item.ToEnum(filePath)).ToImmutableList();
        }
    }
}
