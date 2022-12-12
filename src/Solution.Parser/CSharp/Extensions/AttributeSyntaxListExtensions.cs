using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class AttributeSyntaxListExtensions
    {
        internal static IImmutableList<Attribute> ToAttributes(this SyntaxList<AttributeListSyntax> argSyntaxList)
        {
            return argSyntaxList.SelectMany(list =>
            {
                return list.Attributes.Select(a =>
                {
                    var name = a.Name.ToString();
                    var immutableList = a.ArgumentList?.Arguments.Select(p => p.ToString()).ToImmutableList() ?? ImmutableList<string>.Empty;
                    return new Attribute(name, immutableList);
                }).ToImmutableList();
            }).ToImmutableList();
        }
    }
}
