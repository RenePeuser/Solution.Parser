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
                    return new Attribute(a.ToString(), a.ArgumentList?.Arguments.Select(p => p.ToString()).ToImmutableList() ?? ImmutableList<string>.Empty);
                }).ToImmutableList();
            }).ToImmutableList();
        }
    }
}
