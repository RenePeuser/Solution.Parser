using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Asks the compiler about one <see cref="Invocation"/>: finds its node by the span the record
    /// carries, binds it and translates the answer back into records. Each call is resolved once.
    /// </summary>
    internal static class InvocationResolver
    {
        private static readonly ConditionalWeakTable<Invocation, ResolvedInvocation> Resolved = new();

        internal static ResolvedInvocation Resolve(Invocation call, string member)
        {
            if (Resolved.TryGetValue(call, out var cached))
            {
                return cached;
            }

            if (!SemanticRegistry.TryGet(call.FilePath, out var workspace, out var projectKey))
            {
                throw SymbolsNotLoaded.Exception(member);
            }

            var resolved = Resolve(call, workspace, projectKey);
            Resolved.AddOrUpdate(call, resolved);

            return resolved;
        }

        private static ResolvedInvocation Resolve(Invocation call, Workspace workspace, string projectKey)
        {
            var project = workspace.Compilation(projectKey);

            if (!project.TreesByPath.TryGetValue(call.FilePath, out var tree))
            {
                return ResolvedInvocation.NotFound;
            }

            var span = new TextSpan(call.Location.SpanStart, call.Location.SpanLength);
            var node = tree.GetRoot()
                           .FindNode(span, getInnermostNodeForTie: true)
                           .AncestorsAndSelf()
                           .OfType<InvocationExpressionSyntax>()
                           .FirstOrDefault(n => n.Span == span);

            if (node is null)
            {
                return ResolvedInvocation.NotFound;
            }

            var semanticModel = workspace.SemanticModel(tree, project.Compilation);
            var symbolInfo = semanticModel.GetSymbolInfo(node);
            var operation = semanticModel.GetOperation(node) as IInvocationOperation;
            var symbol = symbolInfo.Symbol as IMethodSymbol;
            var roslyn = new RoslynInvocation(node, semanticModel, symbol, operation);

            var candidates = symbolInfo.CandidateSymbols
                                       .OfType<IMethodSymbol>()
                                       .Select(c => SymbolToModel.ToMethod(c, workspace, project.Compilation))
                                       .ToImmutableList();

            if (symbol is null)
            {
                var status = candidates.IsEmpty ? ResolutionStatus.Unresolved : ResolutionStatus.Ambiguous;

                return new ResolvedInvocation(status, null, candidates, ImmutableList<BoundArgument>.Empty, roslyn);
            }

            var method = SymbolToModel.ToMethod(symbol, workspace, project.Compilation);
            var arguments = operation is null ? ImmutableList<BoundArgument>.Empty : Bind(call, operation, method);

            return new ResolvedInvocation(ResolutionStatus.Resolved, method, candidates, arguments, roslyn);
        }

        /// <summary>
        /// One entry per parameter, in parameter order. The operation already did the hard part: named
        /// arguments in any order, the receiver of an extension method, params arrays and left out
        /// defaults all arrive matched to their parameter.
        /// </summary>
        private static ImmutableList<BoundArgument> Bind(Invocation call, IInvocationOperation operation, Method method)
        {
            return operation.Arguments
                            .Where(a => a.Parameter is not null)
                            .OrderBy(a => a.Parameter!.Ordinal)
                            .Select(a => Bind(call, a, method))
                            .ToImmutableList();
        }

        private static BoundArgument Bind(Invocation call, IArgumentOperation argument, Method method)
        {
            var parameterSymbol = argument.Parameter!;
            var parameter = method.Parameters.FirstOrDefault(p => p.Ordinal == parameterSymbol.Ordinal && p.Name == parameterSymbol.Name)
                            ?? SymbolToModel.ToParameter(parameterSymbol, method.FilePath);

            var constant = argument.Value.ConstantValue;
            var written = argument.Syntax is ArgumentSyntax syntax
                              ? call.Arguments.FirstOrDefault(a => a.Location.SpanStart == syntax.SpanStart)
                              : null;

            var (expression, isExplicit) = argument.ArgumentKind switch
            {
                ArgumentKind.DefaultValue => (null, false),
                ArgumentKind.ParamArray => ParamsExpression(argument.Value),
                _ => (written?.Expression ?? argument.Syntax.ToString(), true)
            };

            return new BoundArgument
            {
                Parameter = parameter,
                Argument = written,
                Expression = expression,
                IsExplicit = isExplicit,
                IsReceiver = written is null && argument.ArgumentKind == ArgumentKind.Explicit && parameterSymbol.Ordinal == 0
                             && parameterSymbol.ContainingSymbol is IMethodSymbol { IsExtensionMethod: true },
                HasConstantValue = constant.HasValue,
                ConstantValue = constant.HasValue ? constant.Value : null
            };
        }

        private static (string? Expression, bool IsExplicit) ParamsExpression(IOperation value)
        {
            var elements = value is IArrayCreationOperation { Initializer: { } initializer }
                               ? initializer.ElementValues.Select(e => e.Syntax.ToString()).ToImmutableList()
                               : ImmutableList<string>.Empty;

            return elements.IsEmpty ? (null, false) : (string.Join(", ", elements), true);
        }
    }
}
