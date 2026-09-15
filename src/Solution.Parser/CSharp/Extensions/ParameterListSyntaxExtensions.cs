using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ParameterListSyntaxExtensions
    {
        internal static ImmutableList<Parameter> ToParameters(this BaseParameterListSyntax? parameterListSyntax, string filePath)
        {
            if (parameterListSyntax is null)
            {
                return ImmutableList<Parameter>.Empty;
            }

            return parameterListSyntax.Parameters.Select((p, i) => p.ToParameter(i, filePath)).ToImmutableList();
        }
    }
}
