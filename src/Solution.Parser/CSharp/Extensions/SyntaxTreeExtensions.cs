using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class SyntaxTreeExtensions
    {
        internal static IImmutableList<T> AllOfType<T>(this SyntaxTree syntaxTree)
            where T : SyntaxNode
        {
            Throw.IfNull(syntaxTree);

            var result = syntaxTree.GetRoot().DescendantNodes().OfType<T>();
            return result.ToImmutableList();
        }


        internal static NameSpace GetNamespace(this SyntaxTree syntaxTree)
        {
            var namespaceDeclarationSyntax = syntaxTree.AllOfType<NamespaceDeclarationSyntax>();
            var fileScopedNamespaceDeclarationSyntaxes = syntaxTree.AllOfType<FileScopedNamespaceDeclarationSyntax>();

            var nameSpace = namespaceDeclarationSyntax.Any() ? namespaceDeclarationSyntax[0].ToNamespace() : fileScopedNamespaceDeclarationSyntaxes[0].ToNamespace();
            return nameSpace;
        }
    }
}
