using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SolutionParser.CSharp
{
    internal static class ParameterListSyntaxExtensions
    {
        internal static IEnumerable<Parameter> ToParameters(this ParameterListSyntax parameterListSyntax)
        {
            Throw.IfNull(() => parameterListSyntax);

            return parameterListSyntax.Parameters.Select(p => p.ToParameter());
        }
    }
}
