using Argument.Check;
using Extensions.Pack;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ParameterSyntaxExtension
    {
        internal static Parameter ToParameter(this ParameterSyntax parameterSyntax,
                                              string filePath)
        {
            Throw.IfNull(parameterSyntax);

            var attributes = parameterSyntax.AttributeLists.ToAttributes(filePath);
            var syntaxTree = parameterSyntax.ToString();
            var defaultValue = parameterSyntax.Default?.Value.ToString();

            return new Parameter(parameterSyntax.Type?.ToString() ?? string.Empty, parameterSyntax.Identifier.Text, attributes, syntaxTree, defaultValue.IsNotNull(), defaultValue, filePath);
        }
    }
}
