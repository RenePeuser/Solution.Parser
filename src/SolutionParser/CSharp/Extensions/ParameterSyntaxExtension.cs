using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SolutionParser.CSharp
{
    internal static class ParameterSyntaxExtension
    {
        internal static Parameter ToParameter(this ParameterSyntax parameterSyntax)
        {
            Throw.IfNull(() => parameterSyntax);

            return new Parameter(parameterSyntax.Type.ToString(), parameterSyntax.Identifier.Text);
        }
    }
}
