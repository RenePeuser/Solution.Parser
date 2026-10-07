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
                       .Select(i => i.ToInvocation(filePath))
                       .ToImmutableList();
        }

        private static Invocation ToInvocation(this InvocationExpressionSyntax invocation, string filePath)
        {
            var (name, target, typeArguments, isConditional) = invocation.Expression.Split();

            return new Invocation
            {
                Name = name,
                FullQualifiedName = target is null ? name : $"{target}.{name}",
                Target = target,
                TypeArguments = typeArguments,
                IsConditional = isConditional,
                Arguments = invocation.ArgumentList.Arguments.Select((a, i) => a.ToInvocationArgument(i, filePath)).ToImmutableList(),
                SyntaxTree = invocation.ToString(),
                FilePath = filePath,
                Location = invocation.ToCodeLocation(filePath)
            };
        }

        private static (string Name, string? Target, ImmutableList<string> TypeArguments, bool IsConditional) Split(this ExpressionSyntax expression)
        {
            return expression switch
            {
                MemberAccessExpressionSyntax memberAccess => (memberAccess.Name.Identifier.ValueText,
                                                              memberAccess.Expression.ToString(),
                                                              memberAccess.Name.ToTypeArguments(),
                                                              false),

                // a?.Go(): the call is the WhenNotNull part, the receiver sits on the enclosing conditional access.
                MemberBindingExpressionSyntax memberBinding => (memberBinding.Name.Identifier.ValueText,
                                                                memberBinding.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>()?.Expression.ToString(),
                                                                memberBinding.Name.ToTypeArguments(),
                                                                true),

                SimpleNameSyntax simpleName => (simpleName.Identifier.ValueText, null, simpleName.ToTypeArguments(), false),

                // A delegate invocation like handlers[0]() or GetAction()() has no name of its own.
                _ => (expression.ToString(), null, ImmutableList<string>.Empty, false)
            };
        }

        private static ImmutableList<string> ToTypeArguments(this SimpleNameSyntax name)
        {
            return name is GenericNameSyntax generic
                       ? generic.TypeArgumentList.Arguments.Select(t => t.ToString()).ToImmutableList()
                       : ImmutableList<string>.Empty;
        }

        private static InvocationArgument ToInvocationArgument(this ArgumentSyntax argument, int ordinal, string filePath)
        {
            return new InvocationArgument
            {
                Ordinal = ordinal,
                Name = argument.NameColon?.Name.Identifier.ValueText,
                Expression = argument.Expression.ToString(),
                RefKind = argument.RefKindKeyword.Kind() switch
                {
                    SyntaxKind.RefKeyword => ArgumentRefKind.Ref,
                    SyntaxKind.OutKeyword => ArgumentRefKind.Out,
                    SyntaxKind.InKeyword => ArgumentRefKind.In,
                    _ => ArgumentRefKind.None
                },
                Location = argument.ToCodeLocation(filePath)
            };
        }
    }
}
