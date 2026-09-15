using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A reference to a resource by key. <see cref="StaticResource"/> and <see cref="DynamicResource"/>
    /// are siblings below it; a dynamic resource is not a kind of static one, which is what the old
    /// hierarchy claimed.
    /// </summary>
    [DebuggerDisplay("{ResourceKey}")]
    public abstract record ResourceReference : MarkupExtension
    {
        internal ResourceReference(string rawValue) : base(rawValue)
        {
        }

        /// <summary>The key being looked up, empty when the extension names none.</summary>
        public required string ResourceKey { get; init; }
    }
}
