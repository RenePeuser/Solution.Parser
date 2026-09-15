using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Every descendant of the given type, crossing type boundaries. Only use it where recursion is
        /// actually wanted; member lists are built from <see cref="DirectMembers{T}"/> instead.
        /// </summary>
        internal static ImmutableList<T> AllOfType<T>(this SyntaxNode syntaxNode)
        {
            Throw.IfNull(syntaxNode);

            return syntaxNode.DescendantNodes().OfType<T>().ToImmutableList();
        }

        /// <summary>
        /// The members declared directly in the type, without descending into nested types.
        /// </summary>
        internal static ImmutableList<T> DirectMembers<T>(this TypeDeclarationSyntax typeDeclaration)
            where T : MemberDeclarationSyntax
        {
            Throw.IfNull(typeDeclaration);

            return typeDeclaration.Members.OfType<T>().ToImmutableList();
        }

        /// <summary>
        /// The type declarations of the file, descending through namespaces but not into types, so that
        /// a nested type is reported by its declaring type only.
        /// </summary>
        internal static ImmutableList<MemberDeclarationSyntax> TopLevelTypeDeclarations(this SyntaxNode root)
        {
            Throw.IfNull(root);

            return Walk(root).ToImmutableList();

            static IEnumerable<MemberDeclarationSyntax> Walk(SyntaxNode node)
            {
                var members = node switch
                {
                    CompilationUnitSyntax compilationUnit => compilationUnit.Members,
                    BaseNamespaceDeclarationSyntax nameSpace => nameSpace.Members,
                    _ => default
                };

                foreach (var member in members)
                {
                    switch (member)
                    {
                        case BaseNamespaceDeclarationSyntax nested:
                            foreach (var type in Walk(nested))
                            {
                                yield return type;
                            }

                            break;
                        case BaseTypeDeclarationSyntax:
                        case DelegateDeclarationSyntax:
                            yield return member;

                            break;
                    }
                }
            }
        }
    }
}
