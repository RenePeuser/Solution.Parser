using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class TypeSyntaxExtensions
    {
        private const string GlobalPrefix = "global::";

        /// <summary>
        /// Takes the written type apart. Without a <paramref name="filePath"/>, as for
        /// <see cref="TypeReference.Parse"/>, the result carries no location.
        /// </summary>
        internal static TypeReference ToTypeReference(this TypeSyntax? type, string? filePath)
        {
            if (type is null)
            {
                return TypeReference.None;
            }

            var source = type.ToSourceCode();
            var location = filePath is null ? CodeLocation.None : type.ToCodeLocation(filePath);

            return type switch
            {
                NullableTypeSyntax nullable => nullable.ElementType.ToTypeReference(filePath) with
                {
                    IsNullable = true,
                    SourceCode = source,
                    Location = location
                },

                PredefinedTypeSyntax predefined => new TypeReference
                {
                    Name = predefined.Keyword.ValueText,
                    IsPredefined = true,
                    SourceCode = source,
                    Location = location
                },

                GenericNameSyntax generic => new TypeReference
                {
                    Name = generic.Identifier.ValueText,
                    TypeArguments = generic.TypeArgumentList.Arguments.Select(a => a.ToTypeReference(filePath)).ToImmutableList(),
                    SourceCode = source,
                    Location = location
                },

                IdentifierNameSyntax identifier => new TypeReference
                {
                    Name = identifier.Identifier.ValueText,
                    SourceCode = source,
                    Location = location
                },

                // System.Collections.Generic.List<int>: the right part is the type, the left part only says where it lives.
                QualifiedNameSyntax qualified => qualified.Right.ToTypeReference(filePath) with
                {
                    Qualifier = StripGlobal(qualified.Left.ToSourceCode()),
                    SourceCode = source,
                    Location = location
                },

                AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.ToTypeReference(filePath) with
                {
                    Qualifier = aliasQualified.Alias.Identifier.ValueText == "global" ? null : aliasQualified.Alias.Identifier.ValueText,
                    SourceCode = source,
                    Location = location
                },

                ArrayTypeSyntax array => ToArray(array.ElementType.ToTypeReference(filePath),
                                                 array.RankSpecifiers.Select(r => (r.Rank, Text: r.ToSourceCode())).ToList(),
                                                 source,
                                                 location),

                TupleTypeSyntax tuple => new TypeReference
                {
                    Name = source,
                    TupleElements = tuple.Elements
                                         .Select(e => new TupleElement(e.Identifier.ValueText.Length > 0 ? e.Identifier.ValueText : null,
                                                                       e.Type.ToTypeReference(filePath)))
                                         .ToImmutableList(),
                    SourceCode = source,
                    Location = location
                },

                // ref int, scoped Span<int>: the type is what follows the keyword.
                RefTypeSyntax reference => reference.Type.ToTypeReference(filePath) with { SourceCode = source, Location = location },

                ScopedTypeSyntax scoped => scoped.Type.ToTypeReference(filePath) with { SourceCode = source, Location = location },

                // Pointers, function pointers and the omitted argument in List<>: the name is the source.
                _ => new TypeReference { Name = source, SourceCode = source, Location = location }
            };
        }

        /// <summary>
        /// The first rank specifier is the outer array: <c>int[][,]</c> is a one dimensional array of
        /// <c>int[,]</c>. So every level holds the specifiers that follow its own.
        /// </summary>
        private static TypeReference ToArray(TypeReference element, IReadOnlyList<(int Rank, string Text)> ranks, string source, CodeLocation location)
        {
            var inner = ranks.Count == 1
                            ? element
                            : ToArray(element, ranks.Skip(1).ToList(), element.SourceCode + string.Concat(ranks.Skip(1).Select(r => r.Text)), location);

            return new TypeReference
            {
                Name = element.Name + string.Concat(ranks.Select(r => r.Text)),
                ArrayRank = ranks[0].Rank,
                ArrayElementType = inner,
                SourceCode = source,
                Location = location
            };
        }

        private static string StripGlobal(string qualifier)
        {
            return qualifier.StartsWith(GlobalPrefix, System.StringComparison.Ordinal) ? qualifier[GlobalPrefix.Length..] : qualifier;
        }
    }
}
