using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class IndexerDeclarationSyntaxExtensions
    {
        internal static Indexer ToIndexer(this IndexerDeclarationSyntax indexerDeclarationSyntax, string filePath)
        {
            const string Name = "this[]";

            return new Indexer
            {
                Name = Name,
                FullQualifiedName = indexerDeclarationSyntax.BuildFullQualifiedName(Name),
                Type = indexerDeclarationSyntax.Type.ToString(),
                Parameters = indexerDeclarationSyntax.ParameterList.ToParameters(filePath),
                Modifiers = indexerDeclarationSyntax.Modifiers.ToModifiers(),
                Accessibility = indexerDeclarationSyntax.Modifiers.ToAccessibility(indexerDeclarationSyntax),
                Attributes = indexerDeclarationSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = indexerDeclarationSyntax.ToDocumentation(),
                Accessors = indexerDeclarationSyntax.AccessorList.ToAccessors(indexerDeclarationSyntax.ExpressionBody, filePath),
                IsExpressionBodied = indexerDeclarationSyntax.ExpressionBody is not null,
                ExplicitInterfaceSpecifier = indexerDeclarationSyntax.ExplicitInterfaceSpecifier?.Name.ToString(),
                SyntaxTree = indexerDeclarationSyntax.ToString(),
                FilePath = filePath,
                Location = indexerDeclarationSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Indexer> ToIndexers(this ImmutableList<IndexerDeclarationSyntax> indexerDeclarationSyntaxes,
                                                          string filePath)
        {
            return indexerDeclarationSyntaxes.Select(i => i.ToIndexer(filePath)).ToImmutableList();
        }
    }
}
