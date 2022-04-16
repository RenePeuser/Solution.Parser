using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class AttributeSyntaxListExtensions
    {
        internal static IEnumerable<Attribute> ToAttributes(this SyntaxList<AttributeListSyntax> argSyntaxList)
        {
            return argSyntaxList.SelectMany(list => list.Attributes.Select(a =>
                new Attribute(a.ToString(), a.ArgumentList?.Arguments.Select(p => p.ToString()).ToList())));
        }
    }
}
