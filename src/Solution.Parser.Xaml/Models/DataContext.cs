using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The <c>DataContext</c> an element sets. Present only when the element actually sets one, so a
    /// null <see cref="ElementBase.DataContext"/> means "inherits from the parent" rather than "set to
    /// an empty string", which is what the old parser reported for every element in the file.
    /// </summary>
    [DebuggerDisplay("{FullQualifiedName}")]
    public record DataContext
    {
        /// <summary>
        /// The type the data context is set to, as far as the markup names it: the type behind
        /// <c>{x:Type ...}</c> or the resource key behind <c>{StaticResource ...}</c>. Empty when the
        /// value names no type, for example a plain <c>{Binding}</c>.
        /// </summary>
        public required string FullQualifiedName { get; init; }

        /// <summary>The value exactly as it was parsed, for anything the name alone does not cover.</summary>
        public required PropertyValue Value { get; init; }
    }
}
