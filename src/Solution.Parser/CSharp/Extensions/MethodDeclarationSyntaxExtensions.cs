using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class MethodDeclarationSyntaxExtensions
    {
        internal static Method ToMethod(this MethodDeclarationSyntax methodDeclarationSyntax, string filePath)
        {
            Throw.IfNull(methodDeclarationSyntax);

            var name = methodDeclarationSyntax.Identifier.ValueText;
            var modifiers = methodDeclarationSyntax.Modifiers.ToModifiers();
            var body = methodDeclarationSyntax.Body.ToMemberBody(methodDeclarationSyntax.ExpressionBody, filePath);
            var hasNoBody = methodDeclarationSyntax.Body is null && methodDeclarationSyntax.ExpressionBody is null;

            return new Method
            {
                Name = name,
                FullQualifiedName = methodDeclarationSyntax.BuildFullQualifiedName(name),
                ReturnParameter = methodDeclarationSyntax.ReturnType.ToString(),
                Parameters = methodDeclarationSyntax.ParameterList.ToParameters(filePath),
                TypeParameters = methodDeclarationSyntax.TypeParameterList.ToTypeParameters(methodDeclarationSyntax.ConstraintClauses, filePath),
                Modifiers = modifiers,
                Accessibility = methodDeclarationSyntax.Modifiers.ToAccessibility(methodDeclarationSyntax),
                Attributes = methodDeclarationSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = methodDeclarationSyntax.ToDocumentation(),
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,
                LocalFunctions = body.LocalFunctions,
                IsExpressionBodied = body.IsExpressionBodied,
                IsIterator = body.IsIterator,
                ExplicitInterfaceSpecifier = methodDeclarationSyntax.ExplicitInterfaceSpecifier?.Name.ToString(),
                IsPartialDefinition = modifiers.Contains(Modifier.Partial) && hasNoBody,
                SyntaxTree = methodDeclarationSyntax.ToString(),
                FilePath = filePath,
                Location = methodDeclarationSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Method> ToMethods(this ImmutableList<MethodDeclarationSyntax> methodDeclarationSyntaxes, string filePath)
        {
            Throw.IfNull(methodDeclarationSyntaxes);

            return methodDeclarationSyntaxes.Select(m => m.ToMethod(filePath)).ToImmutableList();
        }
    }
}
