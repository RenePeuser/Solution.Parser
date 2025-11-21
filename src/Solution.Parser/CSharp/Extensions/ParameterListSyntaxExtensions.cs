using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ParameterListSyntaxExtensions
    {
        internal static ImmutableList<Parameter> ToParameters(this ParameterListSyntax parameterListSyntax,
                                                               string filePath)
        {
            Throw.IfNull(parameterListSyntax);

            return parameterListSyntax.Parameters.Select(p => p.ToParameter(filePath)).ToImmutableList();
        }
    }
}
