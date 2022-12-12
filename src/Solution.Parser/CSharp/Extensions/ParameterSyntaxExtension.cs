using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ParameterSyntaxExtension
    {
        internal static Parameter ToParameter(this ParameterSyntax parameterSyntax)
        {
            Throw.IfNull(parameterSyntax);

            var attributes = parameterSyntax.AttributeLists.ToAttributes();

            return new Parameter(parameterSyntax.Type?.ToString() ?? string.Empty, parameterSyntax.Identifier.Text, attributes);
        }
    }
}
