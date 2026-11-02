using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class InvocationExpressionSyntaxExtensions
    {
        /// <summary>
        /// The calls inside the node, in source order. A local function reports its own calls, so the
        /// search stops there; a lambda has no model of its own, so its calls belong to the member.
        /// </summary>
        internal static ImmutableList<Invocation> ToInvocations(this SyntaxNode? node, string filePath)
        {
            if (node is null)
            {
                return ImmutableList<Invocation>.Empty;
            }

            return node.DescendantNodesAndSelf(descendIntoChildren: child => child == node || child is not LocalFunctionStatementSyntax)
                       .OfType<InvocationExpressionSyntax>()
                       .ToInvocations(filePath);
        }

        /// <summary>The given calls, in the given order, with their receivers linked.</summary>
        internal static ImmutableList<Invocation> ToInvocations(this IEnumerable<InvocationExpressionSyntax> invocations, string filePath)
        {
            // The receiver of a call is built before the call and then shared, so call.Receiver is the
            // very instance the list holds, and a chain can be followed by reference.
            var built = new Dictionary<InvocationExpressionSyntax, Invocation>();

            return invocations.Select(i => i.ToInvocation(filePath, built)).ToImmutableList();
        }

        private static Invocation ToInvocation(this InvocationExpressionSyntax invocation, string filePath, Dictionary<InvocationExpressionSyntax, Invocation> built)
        {
            if (built.TryGetValue(invocation, out var existing))
            {
                return existing;
            }

            var (name, target, typeArguments, isConditional) = invocation.Expression.Split();
            var receiver = invocation.ReceiverCall()?.ToInvocation(filePath, built);

            var result = new Invocation
            {
                Receiver = receiver,
                Name = name,
                FullyQualifiedName = target is null ? name : $"{target}.{name}",
                Target = target,
                TypeArguments = typeArguments,
                IsConditional = isConditional,
                Arguments = invocation.ArgumentList.Arguments.Select((a, i) => a.ToInvocationArgument(i, filePath)).ToImmutableList(),
                SourceCode = invocation.ToSourceCode(),
                FilePath = filePath,
                Location = invocation.ToCodeLocation(filePath)
            };

            built[invocation] = result;

            return result;
        }

        /// <summary>
        /// The call whose result this call is made on: <c>B()</c> in <c>a.B().C()</c> and in
        /// <c>a.B()?.C()</c>. Parentheses and a <c>!</c> in between do not matter.
        /// </summary>
        private static InvocationExpressionSyntax? ReceiverCall(this InvocationExpressionSyntax invocation)
        {
            var receiver = invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
                MemberBindingExpressionSyntax memberBinding => memberBinding.EnclosingConditionalAccess()?.Expression,
                _ => null
            };

            while (receiver is ParenthesizedExpressionSyntax or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
            {
                receiver = receiver switch
                {
                    ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
                    PostfixUnaryExpressionSyntax suppressed => suppressed.Operand,
                    _ => receiver
                };
            }

            return receiver as InvocationExpressionSyntax;
        }

        private static (string Name, string? Target, ImmutableList<string> TypeArguments, bool IsConditional) Split(this ExpressionSyntax expression)
        {
            return expression switch
            {
                MemberAccessExpressionSyntax memberAccess => (memberAccess.Name.Identifier.ValueText,
                                                              memberAccess.Expression.ToSourceCode(),
                                                              memberAccess.Name.ToTypeArguments(),
                                                              false),

                // a?.Go(): the call is the WhenNotNull part, the receiver sits on the enclosing conditional access.
                MemberBindingExpressionSyntax memberBinding => (memberBinding.Name.Identifier.ValueText,
                                                                memberBinding.ConditionalTarget(),
                                                                memberBinding.Name.ToTypeArguments(),
                                                                true),

                SimpleNameSyntax simpleName => (simpleName.Identifier.ValueText, null, simpleName.ToTypeArguments(), false),

                // A delegate invocation like handlers[0]() or GetAction()() has no name of its own.
                _ => (expression.ToSourceCode(), null, ImmutableList<string>.Empty, false)
            };
        }

        private static ImmutableList<string> ToTypeArguments(this SimpleNameSyntax name)
        {
            return name is GenericNameSyntax generic
                       ? generic.TypeArgumentList.Arguments.Select(t => t.ToSourceCode()).ToImmutableList()
                       : ImmutableList<string>.Empty;
        }

        internal static InvocationArgument ToInvocationArgument(this ArgumentSyntax argument, int ordinal, string filePath)
        {
            // One string for both: the text of an argument, a lambda say, can be long.
            var value = argument.Expression.ToExpression(filePath);

            return new InvocationArgument
            {
                Ordinal = ordinal,
                Name = argument.NameColon?.Name.Identifier.ValueText,
                Expression = value.SourceCode,
                ValueExpression = value,
                RefKind = argument.RefKindKeyword.Kind() switch
                {
                    SyntaxKind.RefKeyword => ArgumentRefKind.Ref,
                    SyntaxKind.OutKeyword => ArgumentRefKind.Out,
                    SyntaxKind.InKeyword => ArgumentRefKind.In,
                    _ => ArgumentRefKind.None
                },
                Location = argument.Span == argument.Expression.Span ? value.Location : argument.ToCodeLocation(filePath)
            };
        }
    }
}
