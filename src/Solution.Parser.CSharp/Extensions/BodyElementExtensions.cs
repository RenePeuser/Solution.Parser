using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Collects what a body is made of: object creations, string literals and member accesses. Like the
    /// calls, those inside a lambda belong to the member, while a local function reports its own.
    /// </summary>
    internal static class BodyElementExtensions
    {
        /// <summary>The nodes of the body in source order, without descending into a local function.</summary>
        internal static IEnumerable<SyntaxNode> BodyNodes(this SyntaxNode? node)
        {
            return node is null
                       ? []
                       : node.DescendantNodesAndSelf(descendIntoChildren: child => child == node || child is not LocalFunctionStatementSyntax);
        }

        internal static ImmutableList<ObjectCreation> ToObjectCreations(this SyntaxNode? node, string filePath)
        {
            return node.BodyNodes().OfType<BaseObjectCreationExpressionSyntax>().Select(c => c.ToObjectCreation(filePath)).ToImmutableList();
        }

        internal static ImmutableList<StringLiteral> ToStringLiterals(this SyntaxNode? node, string filePath)
        {
            return node.BodyNodes()
                       .Select(n => n switch
                       {
                           LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression)
                               => literal.ToStringLiteral(filePath),
                           InterpolatedStringExpressionSyntax interpolated => interpolated.ToStringLiteral(filePath),
                           _ => null
                       })
                       .OfType<StringLiteral>()
                       .ToImmutableList();
        }

        internal static ImmutableList<MemberAccess> ToMemberAccesses(this SyntaxNode? node, string filePath)
        {
            return node.BodyNodes()
                       .Select(n => n switch
                       {
                           MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression) => new MemberAccess
                           {
                               Target = access.Expression.ToSourceCode(),
                               Name = access.Name.Identifier.ValueText,
                               IsInvoked = access.Parent is InvocationExpressionSyntax invocation && invocation.Expression == access,
                               SourceCode = access.ToSourceCode(),
                               FilePath = filePath,
                               Location = access.ToCodeLocation(filePath)
                           },
                           MemberBindingExpressionSyntax binding => binding.ToMemberAccess(filePath),
                           _ => null
                       })
                       .OfType<MemberAccess>()
                       .ToImmutableList();
        }

        /// <summary><c>user?.Name</c>: the name is bound here, the target sits on the enclosing conditional access.</summary>
        private static MemberAccess ToMemberAccess(this MemberBindingExpressionSyntax binding, string filePath)
        {
            var target = binding.ConditionalTarget();

            return new MemberAccess
            {
                Target = target ?? string.Empty,
                Name = binding.Name.Identifier.ValueText,
                IsInvoked = binding.Parent is InvocationExpressionSyntax invocation && invocation.Expression == binding,
                IsConditional = true,
                SourceCode = target is null ? binding.ToSourceCode() : $"{target}?{binding.ToSourceCode()}",
                FilePath = filePath,
                Location = binding.ToCodeLocation(filePath)
            };
        }

        private static ObjectCreation ToObjectCreation(this BaseObjectCreationExpressionSyntax creation, string filePath)
        {
            var type = creation is ObjectCreationExpressionSyntax explicitCreation
                           ? explicitCreation.Type.ToTypeReference(filePath)
                           : creation.TargetType(filePath);

            return new ObjectCreation
            {
                TypeReference = type,
                IsTargetTyped = creation is ImplicitObjectCreationExpressionSyntax,
                Arguments = creation.ArgumentList?.Arguments.Select((a, i) => a.ToInvocationArgument(i, filePath)).ToImmutableList()
                            ?? ImmutableList<InvocationArgument>.Empty,
                Initializer = creation.Initializer?.ToSourceCode(),
                InitializedMembers = creation.Initializer is { RawKind: (int)SyntaxKind.ObjectInitializerExpression } initializer
                                         ? initializer.Expressions
                                                      .OfType<AssignmentExpressionSyntax>()
                                                      .Select(a => new InitializedMember(a.Left.ToSourceCode(), a.Right.ToExpression(filePath)))
                                                      .ToImmutableList()
                                         : ImmutableList<InitializedMember>.Empty,
                SourceCode = creation.ToSourceCode(),
                FilePath = filePath,
                Location = creation.ToCodeLocation(filePath)
            };
        }

        /// <summary>
        /// The type a <c>new()</c> gets from its context, where the syntax tells it: the declared type of
        /// the variable, property or parameter it initialises.
        /// </summary>
        private static TypeReference TargetType(this BaseObjectCreationExpressionSyntax creation, string filePath)
        {
            var declared = creation.Parent is EqualsValueClauseSyntax clause
                               ? clause.Parent switch
                               {
                                   VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } => declaration.Type,
                                   PropertyDeclarationSyntax property => property.Type,
                                   ParameterSyntax parameter => parameter.Type,
                                   _ => null
                               }
                               : null;

            var type = declared.ToTypeReference(filePath);

            return type.IsVar ? TypeReference.None : type;
        }

        private static StringLiteral ToStringLiteral(this LiteralExpressionSyntax literal, string filePath)
        {
            return new StringLiteral
            {
                Value = literal.Token.ValueText,
                IsVerbatim = literal.Token.IsVerbatimStringLiteral(),
                IsRaw = literal.Token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken
                            or SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken,
                EnclosingStatement = literal.FirstAncestorOrSelf<StatementSyntax>()?.ToSourceCode(),
                SourceCode = literal.ToSourceCode(),
                FilePath = filePath,
                Location = literal.ToCodeLocation(filePath)
            };
        }

        private static StringLiteral ToStringLiteral(this InterpolatedStringExpressionSyntax interpolated, string filePath)
        {
            var expression = interpolated.ToExpression(filePath);
            var start = interpolated.StringStartToken.Kind();

            return new StringLiteral
            {
                Value = string.Concat(expression.InterpolatedParts.Select(p => p.IsHole ? "{" + p.Text + "}" : p.Text)),
                IsInterpolated = true,
                IsVerbatim = start == SyntaxKind.InterpolatedVerbatimStringStartToken,
                IsRaw = start is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken,
                Parts = expression.InterpolatedParts,
                EnclosingStatement = interpolated.FirstAncestorOrSelf<StatementSyntax>()?.ToSourceCode(),
                SourceCode = interpolated.ToSourceCode(),
                FilePath = filePath,
                Location = interpolated.ToCodeLocation(filePath)
            };
        }
    }
}
