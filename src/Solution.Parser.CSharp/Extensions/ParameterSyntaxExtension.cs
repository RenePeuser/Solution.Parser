using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ParameterSyntaxExtension
    {
        internal static Parameter ToParameter(this ParameterSyntax parameterSyntax, int ordinal, string filePath)
        {
            Throw.IfNull(parameterSyntax);

            var name = parameterSyntax.Identifier.Text;
            var defaultValue = parameterSyntax.Default?.Value.ToSourceCode();

            return new Parameter
            {
                Name = name,
                FullyQualifiedName = name,
                Type = parameterSyntax.Type?.ToSourceCode() ?? string.Empty,
                Attributes = parameterSyntax.AttributeLists.ToAttributes(filePath),
                Modifiers = parameterSyntax.Modifiers.ToModifiers(),
                IsOptional = defaultValue is not null,
                DefaultValue = defaultValue,
                IsParams = parameterSyntax.Modifiers.Any(SyntaxKind.ParamsKeyword),
                IsNullable = parameterSyntax.Type is NullableTypeSyntax,
                Ordinal = ordinal,
                SourceCode = parameterSyntax.ToSourceCode(),
                FilePath = filePath,
                Location = parameterSyntax.ToCodeLocation(filePath)
            };
        }
    }
}
