using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Single translation of Roslyn modifier tokens into <see cref="Modifier"/>. Every declaration
    /// converter shares it, so a modifier never goes missing for one declaration kind only.
    /// </summary>
    internal static class SyntaxTokenListExtensions
    {
        internal static ImmutableList<Modifier> ToModifiers(this SyntaxTokenList tokens)
        {
            var builder = ImmutableList.CreateBuilder<Modifier>();

            foreach (var token in tokens)
            {
                var modifier = ToModifierOrDefault(token);

                if (modifier.HasValue)
                {
                    builder.Add(modifier.Value);
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Resolves the accessibility of a declaration, falling back to the language default when no
        /// accessibility modifier is present. Without this an absent modifier is indistinguishable
        /// from <c>private</c>, which makes rules such as "no public fields" unwritable.
        /// </summary>
        internal static Accessibility ToAccessibility(this SyntaxTokenList tokens, SyntaxNode declaration)
        {
            var isProtected = tokens.Any(SyntaxKind.ProtectedKeyword);
            var isInternal = tokens.Any(SyntaxKind.InternalKeyword);
            var isPrivate = tokens.Any(SyntaxKind.PrivateKeyword);

            if (isProtected && isInternal)
            {
                return Accessibility.ProtectedOrInternal;
            }

            if (isProtected && isPrivate)
            {
                return Accessibility.ProtectedAndInternal;
            }

            if (tokens.Any(SyntaxKind.PublicKeyword))
            {
                return Accessibility.Public;
            }

            if (isProtected)
            {
                return Accessibility.Protected;
            }

            if (isInternal)
            {
                return Accessibility.Internal;
            }

            if (isPrivate)
            {
                return Accessibility.Private;
            }

            return DefaultAccessibility(declaration);
        }

        private static Accessibility DefaultAccessibility(SyntaxNode declaration)
        {
            return declaration switch
            {
                LocalFunctionStatementSyntax => Accessibility.NotApplicable,
                EnumMemberDeclarationSyntax => Accessibility.Public,
                _ => declaration.Parent switch
                {
                    // A member of an interface is public unless stated otherwise.
                    InterfaceDeclarationSyntax => Accessibility.Public,

                    // A member of a class, struct or record is private unless stated otherwise.
                    TypeDeclarationSyntax => Accessibility.Private,

                    // Enum members are always public.
                    EnumDeclarationSyntax => Accessibility.Public,

                    // A type declared directly in a namespace or in the compilation unit is internal.
                    BaseNamespaceDeclarationSyntax or CompilationUnitSyntax => Accessibility.Internal,
                    _ => Accessibility.NotApplicable
                }
            };
        }

        private static Modifier? ToModifierOrDefault(SyntaxToken token)
        {
            return token.Kind() switch
            {
                SyntaxKind.PublicKeyword => Modifier.Public,
                SyntaxKind.InternalKeyword => Modifier.Internal,
                SyntaxKind.ProtectedKeyword => Modifier.Protected,
                SyntaxKind.PrivateKeyword => Modifier.Private,
                SyntaxKind.StaticKeyword => Modifier.Static,
                SyntaxKind.ReadOnlyKeyword => Modifier.ReadOnly,
                SyntaxKind.ConstKeyword => Modifier.Const,
                SyntaxKind.AbstractKeyword => Modifier.Abstract,
                SyntaxKind.PartialKeyword => Modifier.Partial,
                SyntaxKind.RequiredKeyword => Modifier.Required,
                SyntaxKind.SealedKeyword => Modifier.Sealed,
                SyntaxKind.VirtualKeyword => Modifier.Virtual,
                SyntaxKind.OverrideKeyword => Modifier.Override,
                SyntaxKind.NewKeyword => Modifier.New,
                SyntaxKind.AsyncKeyword => Modifier.Async,
                SyntaxKind.ExternKeyword => Modifier.Extern,
                SyntaxKind.UnsafeKeyword => Modifier.Unsafe,
                SyntaxKind.VolatileKeyword => Modifier.Volatile,
                SyntaxKind.FileKeyword => Modifier.File,
                SyntaxKind.FixedKeyword => Modifier.Fixed,
                SyntaxKind.RefKeyword => Modifier.Ref,
                SyntaxKind.InKeyword => Modifier.In,
                SyntaxKind.OutKeyword => Modifier.Out,
                SyntaxKind.ScopedKeyword => Modifier.Scoped,
                _ => null
            };
        }
    }
}
