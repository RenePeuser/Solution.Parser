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
            var parameters = methodDeclarationSyntax.ParameterList.ToParameters(filePath);

            return new Method
            {
                Name = name,
                FullyQualifiedName = methodDeclarationSyntax.BuildFullyQualifiedName(name),
                ReturnParameter = methodDeclarationSyntax.ReturnType.ToSourceCode(),
                ReturnType = methodDeclarationSyntax.ReturnType.ToTypeReference(filePath),
                Parameters = parameters,
                ExtensionReceiver = parameters.FirstOrDefault() is { IsThis: true } receiver ? receiver : null,
                TypeParameters = methodDeclarationSyntax.TypeParameterList.ToTypeParameters(methodDeclarationSyntax.ConstraintClauses, filePath),
                Modifiers = modifiers,
                Accessibility = methodDeclarationSyntax.Modifiers.ToAccessibility(methodDeclarationSyntax),
                Attributes = methodDeclarationSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = methodDeclarationSyntax.ToDocumentation(),
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
                IsIterator = body.IsIterator,
                ExplicitInterfaceSpecifier = methodDeclarationSyntax.ExplicitInterfaceSpecifier?.Name.ToSourceCode(),
                IsPartialDefinition = modifiers.Contains(Modifier.Partial) && hasNoBody,
                SourceCode = methodDeclarationSyntax.ToSourceCode(),
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
