using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class DestructorDeclarationSyntaxExtensions
    {
        internal static Finalizer ToFinalizer(this DestructorDeclarationSyntax destructorSyntax, string filePath)
        {
            var name = destructorSyntax.Identifier.ValueText;
            var body = destructorSyntax.Body.ToMemberBody(destructorSyntax.ExpressionBody, filePath);

            return new Finalizer
            {
                Name = name,
                FullQualifiedName = destructorSyntax.BuildFullQualifiedName($"~{name}"),
                Modifiers = destructorSyntax.Modifiers.ToModifiers(),
                Accessibility = Accessibility.NotApplicable,
                Attributes = destructorSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = destructorSyntax.ToDocumentation(),
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,
                LocalFunctions = body.LocalFunctions,
                Invocations = body.Invocations,
                IsExpressionBodied = body.IsExpressionBodied,
                SyntaxTree = destructorSyntax.ToString(),
                FilePath = filePath,
                Location = destructorSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Finalizer> ToFinalizers(this ImmutableList<DestructorDeclarationSyntax> destructorSyntaxes, string filePath)
        {
            return destructorSyntaxes.Select(d => d.ToFinalizer(filePath)).ToImmutableList();
        }
    }
}
