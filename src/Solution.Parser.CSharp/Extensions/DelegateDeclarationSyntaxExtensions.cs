using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class DelegateDeclarationSyntaxExtensions
    {
        internal static Delegate ToDelegate(this DelegateDeclarationSyntax delegateSyntax, string filePath)
        {
            Throw.IfNull(delegateSyntax);

            var name = delegateSyntax.Identifier.ValueText;

            return new Delegate
            {
                Name = name,
                FullQualifiedName = delegateSyntax.BuildFullQualifiedName(name),
                NameSpace = delegateSyntax.NamespaceOf(),
                ReturnParameter = delegateSyntax.ReturnType.ToString(),
                Parameters = delegateSyntax.ParameterList.ToParameters(filePath),
                TypeParameters = delegateSyntax.TypeParameterList.ToTypeParameters(delegateSyntax.ConstraintClauses, filePath),
                Modifiers = delegateSyntax.Modifiers.ToModifiers(),
                Accessibility = delegateSyntax.Modifiers.ToAccessibility(delegateSyntax),
                Attributes = delegateSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = delegateSyntax.ToDocumentation(),
                SyntaxTree = delegateSyntax.ToString(),
                FilePath = filePath,
                Location = delegateSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Delegate> ToDelegates(this ImmutableList<DelegateDeclarationSyntax> delegateSyntaxes, string filePath)
        {
            return delegateSyntaxes.Select(d => d.ToDelegate(filePath)).ToImmutableList();
        }
    }
}
