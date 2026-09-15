using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class LocalFunctionDeclarationSyntaxExtensions
    {
        internal static LocalFunction ToLocalFunction(this LocalFunctionStatementSyntax localFunctionSyntax, string filePath)
        {
            Throw.IfNull(localFunctionSyntax);

            var name = localFunctionSyntax.Identifier.ValueText;
            var body = localFunctionSyntax.Body.ToMemberBody(localFunctionSyntax.ExpressionBody, filePath);

            return new LocalFunction
            {
                Name = name,
                FullQualifiedName = localFunctionSyntax.BuildFullQualifiedName(name),
                ReturnParameter = localFunctionSyntax.ReturnType.ToString(),
                Parameters = localFunctionSyntax.ParameterList.ToParameters(filePath),
                TypeParameters = localFunctionSyntax.TypeParameterList.ToTypeParameters(localFunctionSyntax.ConstraintClauses, filePath),
                Modifiers = localFunctionSyntax.Modifiers.ToModifiers(),
                Accessibility = Accessibility.NotApplicable,
                Attributes = localFunctionSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = localFunctionSyntax.ToDocumentation(),
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,

                // A nested local function is reported once, by the enclosing member.
                LocalFunctions = ImmutableList<LocalFunction>.Empty,
                IsExpressionBodied = body.IsExpressionBodied,
                IsIterator = body.IsIterator,
                SyntaxTree = localFunctionSyntax.ToString(),
                FilePath = filePath,
                Location = localFunctionSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<LocalFunction> ToLocalFunctions(this ImmutableList<LocalFunctionStatementSyntax> localFunctionSyntaxes,
                                                                     string filePath)
        {
            Throw.IfNull(localFunctionSyntaxes);

            return localFunctionSyntaxes.Select(l => l.ToLocalFunction(filePath)).ToImmutableList();
        }
    }
}
