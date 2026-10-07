using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class OperatorDeclarationSyntaxExtensions
    {
        internal static Operator ToOperator(this OperatorDeclarationSyntax operatorSyntax, string filePath)
        {
            var symbol = operatorSyntax.OperatorToken.Text;
            var name = $"operator {symbol}";
            var body = operatorSyntax.Body.ToMemberBody(operatorSyntax.ExpressionBody, filePath);

            return new Operator
            {
                Name = name,
                FullQualifiedName = operatorSyntax.BuildFullQualifiedName(name),
                ReturnParameter = operatorSyntax.ReturnType.ToString(),
                Symbol = symbol,
                OperatorKind = OperatorKind.Operator,
                IsCheckedOperator = operatorSyntax.CheckedKeyword.IsKind(SyntaxKind.CheckedKeyword),
                Parameters = operatorSyntax.ParameterList.ToParameters(filePath),
                Modifiers = operatorSyntax.Modifiers.ToModifiers(),
                Accessibility = operatorSyntax.Modifiers.ToAccessibility(operatorSyntax),
                Attributes = operatorSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = operatorSyntax.ToDocumentation(),
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,
                LocalFunctions = body.LocalFunctions,
                Invocations = body.Invocations,
                IsExpressionBodied = body.IsExpressionBodied,
                IsIterator = body.IsIterator,
                SyntaxTree = operatorSyntax.ToString(),
                FilePath = filePath,
                Location = operatorSyntax.ToCodeLocation(filePath)
            };
        }

        internal static Operator ToOperator(this ConversionOperatorDeclarationSyntax conversionSyntax, string filePath)
        {
            var isImplicit = conversionSyntax.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ImplicitKeyword);
            var target = conversionSyntax.Type.ToString();
            var name = $"{(isImplicit ? "implicit" : "explicit")} operator {target}";
            var body = conversionSyntax.Body.ToMemberBody(conversionSyntax.ExpressionBody, filePath);

            return new Operator
            {
                Name = name,
                FullQualifiedName = conversionSyntax.BuildFullQualifiedName(name),
                ReturnParameter = target,
                Symbol = target,
                OperatorKind = isImplicit ? OperatorKind.ImplicitConversion : OperatorKind.ExplicitConversion,
                IsCheckedOperator = conversionSyntax.CheckedKeyword.IsKind(SyntaxKind.CheckedKeyword),
                Parameters = conversionSyntax.ParameterList.ToParameters(filePath),
                Modifiers = conversionSyntax.Modifiers.ToModifiers(),
                Accessibility = conversionSyntax.Modifiers.ToAccessibility(conversionSyntax),
                Attributes = conversionSyntax.AttributeLists.ToAttributes(filePath),
                Documentation = conversionSyntax.ToDocumentation(),
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,
                LocalFunctions = body.LocalFunctions,
                Invocations = body.Invocations,
                IsExpressionBodied = body.IsExpressionBodied,
                IsIterator = body.IsIterator,
                SyntaxTree = conversionSyntax.ToString(),
                FilePath = filePath,
                Location = conversionSyntax.ToCodeLocation(filePath)
            };
        }

        internal static ImmutableList<Operator> ToOperators(this TypeDeclarationSyntax typeDeclarationSyntax, string filePath)
        {
            var operators = typeDeclarationSyntax.DirectMembers<OperatorDeclarationSyntax>().Select(o => o.ToOperator(filePath));
            var conversions = typeDeclarationSyntax.DirectMembers<ConversionOperatorDeclarationSyntax>().Select(c => c.ToOperator(filePath));

            return operators.Concat(conversions).ToImmutableList();
        }
    }
}
