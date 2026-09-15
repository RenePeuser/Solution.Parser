using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A <c>{MultiBinding ...}</c>. The inner bindings are usually written as child elements rather
    /// than as arguments; those are reachable through the owning element and are not repeated here.
    /// </summary>
    [DebuggerDisplay("{ValueText}")]
    public record MultiBinding : MarkupExtension
    {
        internal MultiBinding(string rawValue) : base(rawValue)
        {
        }

        public PropertyValue? Converter { get; init; }

        public PropertyValue? ConverterParameter { get; init; }

        public string Mode { get; init; } = "Default";

        public StringFormat? StringFormat { get; init; }

        /// <summary>The bindings given inline as arguments, empty when they are written as child elements.</summary>
        public ImmutableList<Binding> Bindings { get; init; } = ImmutableList<Binding>.Empty;
    }
}
