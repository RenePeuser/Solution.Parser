using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class FileScopedNamespaceDeclarationSyntaxExtensions
    {
        internal static Namespace ToNamespace(this FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
        {
            Throw.IfNull(fileScopedNamespaceDeclarationSyntax);

            var fullqualifiedName = fileScopedNamespaceDeclarationSyntax.Name.ToString();
            return new Namespace(fullqualifiedName);
        }
    }
}
