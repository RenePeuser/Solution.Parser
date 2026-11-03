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
                FullyQualifiedName = destructorSyntax.BuildFullyQualifiedName($"~{name}"),
                Modifiers = destructorSyntax.Modifiers.ToModifiers(),
                Accessibility = Accessibility.NotApplicable,
                Attributes = destructorSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = destructorSyntax.ToDocumentation(),
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,
                LocalFunctions = body.LocalFunctions,
                Invocations = body.Invocations,
                LocalVariables = body.LocalVariables,
                ObjectCreations = body.ObjectCreations,
                StringLiterals = body.StringLiterals,
                MemberAccesses = body.MemberAccesses,
                BodyStatements = body.BodyStatements,
                AllStatements = body.AllStatements,
                SwitchExpressions = body.SwitchExpressions,
                Lambdas = body.Lambdas,
                Awaits = body.Awaits,
                ExpressionBody = body.ExpressionBody,
                IsExpressionBodied = body.IsExpressionBodied,
                SourceCode = destructorSyntax.ToSourceCode(),
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
