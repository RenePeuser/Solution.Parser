using System;
using System.Linq;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A string written in a body: plain, verbatim, raw, <c>u8</c> or interpolated.
    /// </summary>
    /// <example>
    /// <code>
    /// from literal in method.StringLiterals
    /// where literal.Value.Contains("SELECT", StringComparison.OrdinalIgnoreCase) &amp;&amp; literal.IsInterpolated
    /// select $"{literal.Location}: SQL built by interpolation";
    /// </code>
    /// </example>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record StringLiteral : CodeElement
    {
        /// <summary>
        /// The text the compiler sees, without quotes and escapes. For an interpolated string the
        /// holes keep their source, <c>api/{version}</c> for <c>$"api/{version}"</c>.
        /// </summary>
        public required string Value { get; init; }

        public bool IsInterpolated { get; init; }

        /// <summary>True for <c>@"..."</c>, also combined with <c>$</c>.</summary>
        public bool IsVerbatim { get; init; }

        /// <summary>True for <c>"""..."""</c>, also combined with <c>$</c>.</summary>
        public bool IsRaw { get; init; }

        /// <summary>The parts of an interpolated string; empty for a plain one.</summary>
        public ImmutableList<InterpolatedStringPart> Parts { get; init; } = ImmutableList<InterpolatedStringPart>.Empty;

        private string? _enclosingStatement;

        private readonly SourceSlice? _enclosingStatementSlice;

        /// <summary>The source of the statement the string is written in, for quoting it in a finding; null outside a statement.</summary>
        public string? EnclosingStatement
        {
            get => _enclosingStatement ??= _enclosingStatementSlice?.ToString();
            init => _enclosingStatement = value;
        }

        /// <summary>Sets <see cref="EnclosingStatement"/> by its node, cut out on first use.</summary>
        internal Microsoft.CodeAnalysis.SyntaxNode? EnclosingStatementNode
        {
            init => _enclosingStatementSlice = value is null ? null : SourceSlice.Of(value);
        }

        /// <summary>
        /// Equal by what the string says, not by how far its text was cut out yet; the generated
        /// equality would compare the cached statement and the file text it is cut from.
        /// </summary>
        public bool Equals(StringLiteral? other)
        {
            return base.Equals(other) &&
                   Value == other.Value &&
                   IsInterpolated == other.IsInterpolated &&
                   IsVerbatim == other.IsVerbatim &&
                   IsRaw == other.IsRaw &&
                   Parts.SequenceEqual(other.Parts) &&
                   EnclosingStatement == other.EnclosingStatement;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Value);
        }
    }
}
