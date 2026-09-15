using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Single implementation of the fully qualified name. It walks all the way up to the compilation
    /// unit, so nested types, nested namespaces and file scoped namespaces are all handled.
    /// </summary>
    internal static class QualifiedNameExtensions
    {
        internal static string BuildFullQualifiedName(this SyntaxNode declaration, string name)
        {
            var qualifiers = EnclosingQualifiers(declaration).Reverse().ToList();
            qualifiers.Add(name);

            return string.Join(".", qualifiers.Where(q => !string.IsNullOrEmpty(q)));
        }

        /// <summary>
        /// The namespace the declaration lives in, or an empty namespace when it lives in none.
        /// Resolved per declaration rather than per file, because a file may hold several namespaces.
        /// </summary>
        internal static NameSpace NamespaceOf(this SyntaxNode declaration)
        {
            var names = new List<string>();

            for (var parent = declaration.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is BaseNamespaceDeclarationSyntax nameSpace)
                {
                    names.Add(nameSpace.Name.ToString());
                }
            }

            names.Reverse();

            return new NameSpace(string.Join(".", names));
        }

        /// <summary>
        /// Walks outwards from the declaration and yields the name of every enclosing type, method and
        /// namespace, innermost first.
        /// </summary>
        private static IEnumerable<string> EnclosingQualifiers(SyntaxNode declaration)
        {
            for (var parent = declaration.Parent; parent is not null; parent = parent.Parent)
            {
                switch (parent)
                {
                    // Covers class, struct, record, record struct, interface and enum.
                    case BaseTypeDeclarationSyntax type:
                        yield return type.Identifier.ValueText;

                        break;

                    // Covers both the block scoped and the file scoped namespace.
                    case BaseNamespaceDeclarationSyntax nameSpace:
                        yield return nameSpace.Name.ToString();

                        break;

                    // Enclosing members of a local function.
                    case MethodDeclarationSyntax method:
                        yield return method.Identifier.ValueText;

                        break;
                    case LocalFunctionStatementSyntax localFunction:
                        yield return localFunction.Identifier.ValueText;

                        break;
                    case ConstructorDeclarationSyntax constructor:
                        yield return constructor.Identifier.ValueText;

                        break;
                    case PropertyDeclarationSyntax property:
                        yield return property.Identifier.ValueText;

                        break;
                }
            }
        }
    }
}
