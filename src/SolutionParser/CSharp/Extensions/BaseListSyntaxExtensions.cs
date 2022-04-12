using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SolutionParser.CSharp
{
    internal static class BaseListSyntaxExtensions
    {
        internal static IEnumerable<BaseType> ToBaseTypes(this BaseListSyntax baseListSyntax)
        {
            Throw.IfNull(() => baseListSyntax);

            return baseListSyntax.Types.Select(type => new BaseType(type.Type.ToString()));
        }
    }
}
