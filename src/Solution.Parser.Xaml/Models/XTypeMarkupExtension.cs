using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>An <c>{x:Type SomeType}</c>.</summary>
    [DebuggerDisplay("{Type}")]
    public record XTypeMarkupExtension : MarkupExtension
    {
        internal XTypeMarkupExtension(string rawValue) : base(rawValue)
        {
        }

        /// <summary>The named type with any prefix stripped.</summary>
        public required string Type { get; init; }
    }
}
