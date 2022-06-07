using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class BaseListSyntaxExtensions
    {
        internal static IImmutableList<BaseType> ToBaseTypes(this BaseListSyntax baseListSyntax)
        {
            Throw.IfNull(baseListSyntax);

            return baseListSyntax.Types.Select(type => new BaseType(type.Type.ToString())).ToImmutableList();
        }
    }
}
