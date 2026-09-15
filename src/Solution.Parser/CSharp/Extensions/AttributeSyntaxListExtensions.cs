using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class AttributeSyntaxListExtensions
    {
        internal static ImmutableList<Attribute> ToAttributes(this SyntaxList<AttributeListSyntax> attributeLists, string filePath)
        {
            return attributeLists.SelectMany(list => list.Attributes.Select(a => a.ToAttribute(list, filePath))).ToImmutableList();
        }

        private static Attribute ToAttribute(this AttributeSyntax attribute, AttributeListSyntax list, string filePath)
        {
            var arguments = attribute.ArgumentList?.Arguments ?? default;
            var name = attribute.Name.ToString();

            var positional = arguments.Where(a => a.NameEquals is null && a.NameColon is null)
                                      .Select(a => a.Expression.ToString())
                                      .ToImmutableList();

            var named = arguments.Where(a => a.NameEquals is not null || a.NameColon is not null)
                                 .Select(a => new AttributeArgument(a.NameEquals?.Name.Identifier.ValueText ??
                                                                    a.NameColon?.Name.Identifier.ValueText ??
                                                                    string.Empty,
                                                                    a.Expression.ToString()))
                                 .ToImmutableList();

            return new Attribute
            {
                Name = name,
                FullQualifiedName = attribute.BuildFullQualifiedName(name),
                Arguments = arguments.Select(a => a.ToString()).ToImmutableList(),
                PositionalArguments = positional,
                NamedArguments = named,
                Target = list.Target?.Identifier.ValueText,
                SyntaxTree = attribute.ToString(),
                FilePath = filePath,
                Location = attribute.ToCodeLocation(filePath)
            };
        }
    }
}
