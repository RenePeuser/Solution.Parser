using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SolutionParser.CSharp
{
    internal static class NamespaceDeclarationSyntaxExtensions
    {
        internal static NameSpace ToNamespace(this NamespaceDeclarationSyntax namespaceDeclarationSyntax)
        {
            Throw.IfNull(() => namespaceDeclarationSyntax);

            var fullqualifiedName = namespaceDeclarationSyntax.Name.ToString();
            return new NameSpace(fullqualifiedName);
        }
    }
}
