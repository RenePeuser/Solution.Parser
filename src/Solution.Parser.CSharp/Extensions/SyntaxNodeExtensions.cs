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
        /// What <c>node.ToString()</c> gives, sliced out of the text the tree was parsed from. One string,
        /// where <c>ToString()</c> writes every token through a fresh StringBuilder; the models keep a lot
        /// of source text, so this is most of what parsing a file allocates.
        /// </summary>
        internal static string ToSourceCode(this SyntaxNode node)
        {
            return node.SyntaxTree.TryGetText(out var text) ? text.ToString(node.Span) : node.ToString();
        }

        /// <summary>
        /// The <c>?.</c> a member binding hangs on. In <c>a?.B()?.C()</c> the binding <c>.B</c> sits in the
        /// left part of the inner conditional access, so the nearest one is not necessarily its own: it is
        /// the nearest one whose right part holds the binding.
        /// </summary>
        internal static ConditionalAccessExpressionSyntax? EnclosingConditionalAccess(this MemberBindingExpressionSyntax binding)
        {
            return binding.Ancestors()
                          .OfType<ConditionalAccessExpressionSyntax>()
                          .FirstOrDefault(c => c.WhenNotNull.Span.Contains(binding.Span));
        }

        /// <summary>
        /// What a member binding is accessed on, as written: <c>source?.Load()</c> for <c>.Parse</c> in
        /// <c>source?.Load()?.Parse()</c>. It runs from the start of the whole <c>?.</c> chain up to the
        /// binding's own <c>?</c>.
        /// </summary>
        internal static string? ConditionalTarget(this MemberBindingExpressionSyntax binding)
        {
            var own = binding.EnclosingConditionalAccess();

            if (own is null)
            {
                return null;
            }

            var top = own;

            while (top.Parent is ConditionalAccessExpressionSyntax parent && parent.WhenNotNull == top)
            {
                top = parent;
            }

            return binding.SyntaxTree.GetText().ToString(Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(top.SpanStart, own.OperatorToken.SpanStart));
        }

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
