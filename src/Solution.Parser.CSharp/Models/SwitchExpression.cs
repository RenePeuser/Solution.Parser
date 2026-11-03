using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// <c>subject switch { pattern =&gt; result, _ =&gt; fallback }</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// from switchExpression in method.SwitchExpressions
    /// where !switchExpression.HasDiscardArm
    /// select $"{switchExpression.Location}: add a '_ =&gt;' arm";
    /// </code>
    /// </example>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record SwitchExpression : CodeElement
    {
        /// <summary>What is switched on.</summary>
        public required Expression Subject { get; init; }

        public ImmutableList<SwitchExpressionArm> Arms { get; init; } = ImmutableList<SwitchExpressionArm>.Empty;

        /// <summary>True when an arm is the catch all <c>_ =&gt;</c>.</summary>
        public bool HasDiscardArm => Arms.Any(a => a.IsDiscard);
    }

    /// <summary>One <c>pattern when condition =&gt; result</c> of a <see cref="SwitchExpression"/>.</summary>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record SwitchExpressionArm : CodeElement
    {
        /// <summary>The pattern as written, <c>&gt; 10</c>, <c>Foo { Id: 1 }</c> or <c>_</c>.</summary>
        public required string Pattern { get; init; }

        /// <summary>The pattern taken apart, see <see cref="CSharp.Pattern"/>; <see cref="Pattern"/> is the same as text.</summary>
        public required CSharp.Pattern TypedPattern { get; init; }

        /// <summary>The condition after <c>when</c>; null when there is none.</summary>
        public Expression? WhenClause { get; init; }

        public required Expression Result { get; init; }

        /// <summary>True for the catch all <c>_</c> without a <c>when</c>.</summary>
        public bool IsDiscard { get; init; }
    }
}
