using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// An <c>{x:Null}</c>. <see cref="PropertyValue.Value"/> is null, and the distinct type lets a rule
    /// tell an explicit null apart from a property that was never set.
    /// </summary>
    [DebuggerDisplay("x:Null")]
    public record NullExtension : MarkupExtension
    {
        internal NullExtension(string rawValue) : base((object?)null)
        {
            RawValue = rawValue;
        }

        /// <summary>The extension exactly as written. <see cref="PropertyValue.Value"/> is null by design.</summary>
        public string RawValue { get; }
    }
}
