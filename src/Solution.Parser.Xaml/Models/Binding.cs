using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A <c>{Binding ...}</c>. Every argument keeps the shape it was written in, so
    /// <c>Converter={StaticResource Bool}</c> comes back as a <see cref="StaticResource"/> rather
    /// than as text. Arguments the binding does not set are null, except the two WPF gives a default.
    /// </summary>
    [DebuggerDisplay("{ValueText}")]
    public record Binding : MarkupExtension
    {
        internal Binding(string rawValue) : base(rawValue)
        {
        }

        /// <summary>
        /// The bound path. <c>{Binding}</c> and the shorthand <c>{Binding Foo}</c> both end up here, so
        /// a rule does not have to know which spelling was used.
        /// </summary>
        public PropertyValue? Path { get; init; }

        public PropertyValue? Source { get; init; }

        public PropertyValue? Converter { get; init; }

        public PropertyValue? ConverterParameter { get; init; }

        /// <summary>The binding mode, <c>Default</c> when the binding does not set one.</summary>
        public string Mode { get; init; } = "Default";

        /// <summary>The update source trigger, <c>Default</c> when the binding does not set one.</summary>
        public string UpdateSourceTrigger { get; init; } = "Default";

        public string ElementName { get; init; } = string.Empty;

        public RelativeSource? RelativeSource { get; init; }

        public PropertyValue? FallbackValue { get; init; }

        public PropertyValue? TargetNullValue { get; init; }

        public StringFormat? StringFormat { get; init; }
    }
}
