using System.Diagnostics;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// An <c>{x:Static SomeType.Member}</c>. The old parser produced an
    /// <see cref="XTypeMarkupExtension"/> here, which made this type unreachable.
    /// </summary>
    [DebuggerDisplay("{FullQualifiedName}")]
    public record XStaticMarkupExtension : MarkupExtension
    {
        internal XStaticMarkupExtension(string rawValue) : base(rawValue)
        {
        }

        /// <summary>The declaring type with any prefix stripped, empty when the member is named on its own.</summary>
        public required string Type { get; init; }

        /// <summary>The member being read.</summary>
        public required string Member { get; init; }

        public string FullQualifiedName => Type.IsNullOrEmpty() ? Member : $"{Type}.{Member}";
    }
}
