using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Solution.Parser.CSharp
{
    public enum ResolutionStatus
    {
        /// <summary>The compiler bound the call to exactly one method.</summary>
        Resolved,

        /// <summary>Several overloads fit and none wins, or the best one does not fit; see the compilation diagnostics.</summary>
        Ambiguous,

        /// <summary>No method was found, typically because a reference is missing. Did the solution get restored?</summary>
        Unresolved
    }

    public enum SymbolOriginKind
    {
        /// <summary>Declared in the code base itself.</summary>
        Source,

        /// <summary>Comes from a NuGet package.</summary>
        Package,

        /// <summary>Comes from a shared framework, like <c>System.Linq</c>.</summary>
        Framework,

        Unknown
    }

    /// <summary>Where a symbol is declared: in the code base, in a package or in the framework.</summary>
    [DebuggerDisplay("{Kind} {Name} {Version}")]
    public sealed record SymbolOrigin(SymbolOriginKind Kind, string Name, string Version, string Path)
    {
        public static SymbolOrigin Source { get; } = new(SymbolOriginKind.Source, string.Empty, string.Empty, string.Empty);

        public static SymbolOrigin Unknown { get; } = new(SymbolOriginKind.Unknown, string.Empty, string.Empty, string.Empty);

        public bool IsSource => Kind == SymbolOriginKind.Source;

        public bool IsPackage => Kind == SymbolOriginKind.Package;

        public bool IsFramework => Kind == SymbolOriginKind.Framework;
    }

    /// <summary>
    /// A parameter of the called method together with what the call passes for it. This is what the
    /// syntax alone cannot tell: <c>true</c> in <c>AssertPostAsync("..", "..", true)</c> is
    /// <c>writeResponse</c>.
    /// </summary>
    [DebuggerDisplay("{Parameter.Name} = {Expression}")]
    public sealed record BoundArgument
    {
        public required Parameter Parameter { get; init; }

        /// <summary>The argument as written; null when the caller left it out or when it is the receiver of an extension method.</summary>
        public InvocationArgument? Argument { get; init; }

        /// <summary>
        /// The source of what is passed: the argument, the receiver of an extension method, the
        /// elements of a <c>params</c> array joined by comma, or null when the default value applies.
        /// </summary>
        public string? Expression { get; init; }

        /// <summary>False when the caller left the argument out and the default value of the parameter applies.</summary>
        public bool IsExplicit { get; init; }

        public bool IsNamed => Argument?.IsNamed ?? false;

        /// <summary>True for the <c>this</c> parameter of an extension method called as <c>receiver.Method()</c>.</summary>
        public bool IsReceiver { get; init; }

        /// <summary>True when the compiler knows the value, for a literal, a <c>const</c>, a <c>nameof</c> or a left out default.</summary>
        public bool HasConstantValue { get; init; }

        public object? ConstantValue { get; init; }

        /// <summary>
        /// True when the compiler knows the value and it equals <paramref name="value"/>. A left out
        /// argument counts with its default value, so <c>Is(true)</c> also finds the calls that rely on
        /// <c>writeResponse = true</c>.
        /// </summary>
        public bool Is(object? value)
        {
            return HasConstantValue && Equals(ConstantValue, value);
        }
    }

    /// <summary>The way out to Roslyn, for whatever this API does not cover.</summary>
    public sealed record RoslynInvocation(InvocationExpressionSyntax Node,
                                          SemanticModel SemanticModel,
                                          IMethodSymbol? Symbol,
                                          IInvocationOperation? Operation);

    /// <summary>Everything the compiler says about one call, computed once.</summary>
    internal sealed record ResolvedInvocation(ResolutionStatus Status,
                                              Method? Method,
                                              ImmutableList<Method> Candidates,
                                              ImmutableList<BoundArgument> Arguments,
                                              RoslynInvocation? Roslyn)
    {
        internal static ResolvedInvocation NotFound { get; } = new(ResolutionStatus.Unresolved,
                                                                   null,
                                                                   ImmutableList<Method>.Empty,
                                                                   ImmutableList<BoundArgument>.Empty,
                                                                   null);
    }
}
