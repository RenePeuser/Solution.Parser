using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A lambda, <c>x =&gt; x.Id</c> or <c>async (a, b) =&gt; { ... }</c>, or an anonymous method,
    /// <c>delegate { ... }</c>. Its calls, variables and statements also belong to the member it is
    /// written in.
    /// </summary>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record Lambda : CodeElement
    {
        public ImmutableList<Parameter> Parameters { get; init; } = ImmutableList<Parameter>.Empty;

        public bool IsAsync { get; init; }

        /// <summary>True for <c>static x =&gt; ...</c>, which cannot capture anything.</summary>
        public bool IsStatic { get; init; }

        /// <summary>True for <c>x =&gt; expression</c> rather than a block.</summary>
        public bool IsExpressionBodied { get; init; }

        private string? _body;

        private readonly SourceSlice _bodySlice;

        /// <summary>The block with its braces, or the expression.</summary>
        public string Body
        {
            get => _body ??= _bodySlice.ToString();
            init => _body = value;
        }

        /// <summary>Sets <see cref="Body"/> by its node, cut out on first use.</summary>
        internal Microsoft.CodeAnalysis.SyntaxNode BodyNode
        {
            init => _bodySlice = SourceSlice.Of(value);
        }

        /// <summary>The statements of a block body; empty for an expression bodied lambda.</summary>
        public ImmutableList<Statement> Statements { get; init; } = ImmutableList<Statement>.Empty;

        /// <summary>The method calls inside the lambda, in source order.</summary>
        public ImmutableList<Invocation> Invocations { get; init; } = ImmutableList<Invocation>.Empty;
    }
}
