using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class UsingDirectiveSyntaxExtensions
    {
        internal static ImmutableList<Using> ToUsings(this SyntaxNode root, string filePath)
        {
            return root.AllOfType<UsingDirectiveSyntax>().Select(u => u.ToUsing(filePath)).ToImmutableList();
        }

        private static Using ToUsing(this UsingDirectiveSyntax directive, string filePath)
        {
            // NamespaceOrType covers an alias to a type, which Name does not.
            var value = directive.NamespaceOrType?.ToString() ?? directive.Name?.ToString() ?? string.Empty;

            return new Using(value)
            {
                Alias = directive.Alias?.Name.Identifier.ValueText,
                IsGlobal = directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword),
                IsStatic = directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword),
                Location = directive.ToCodeLocation(filePath)
            };
        }
    }
}
