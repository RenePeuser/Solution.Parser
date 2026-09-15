using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A <c>{RelativeSource ...}</c>. The mode is read from the positional argument or from
    /// <c>Mode=</c>, and <c>AncestorType</c> keeps its own field instead of being folded into the
    /// mode text.
    /// </summary>
    [DebuggerDisplay("{Mode} {AncestorType}")]
    public record RelativeSource : MarkupExtension
    {
        internal RelativeSource(string rawValue) : base(rawValue)
        {
        }

        /// <summary><c>Self</c>, <c>TemplatedParent</c>, <c>FindAncestor</c> or <c>PreviousData</c>.</summary>
        public string Mode { get; init; } = string.Empty;

        /// <summary>The type to search for, with any prefix stripped. Empty unless the mode is <c>FindAncestor</c>.</summary>
        public string AncestorType { get; init; } = string.Empty;

        /// <summary>Which matching ancestor to take, 1 when the extension does not say.</summary>
        public int AncestorLevel { get; init; } = 1;
    }
}
