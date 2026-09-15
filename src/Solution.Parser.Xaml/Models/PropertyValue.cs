using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The value of a <see cref="Property"/>. A plain attribute value stays a
    /// <see cref="PropertyValue"/>; anything written as <c>{...}</c> becomes a
    /// <see cref="MarkupExtension"/> or one of its flavours.
    /// </summary>
    [DebuggerDisplay("{ValueText}")]
    public record PropertyValue
    {
        internal PropertyValue(object? value)
        {
            Value = value;
            ValueText = value?.ToString() ?? string.Empty;
        }

        public object? Value { get; }

        /// <summary>The value as text, empty rather than null when there is no value.</summary>
        public string ValueText { get; }
    }
}
