using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A value written as <c>{Name argument, Property=Value}</c>. <see cref="Binding"/>,
    /// <see cref="StaticResource"/>, <see cref="RelativeSource"/> and the rest are flavours of it, so
    /// a rule can ask for every markup extension on an element and then narrow down.
    /// </summary>
    [DebuggerDisplay("{FullName,nq}")]
    public record MarkupExtension : PropertyValue
    {
        internal MarkupExtension(string rawValue) : base(rawValue)
        {
        }

        /// <summary>For an extension whose value is not its own text, such as <c>{x:Null}</c>.</summary>
        private protected MarkupExtension(object? value) : base(value)
        {
        }

        /// <summary>The extension name without its prefix and without the <c>Extension</c> suffix, for example <c>Binding</c>.</summary>
        public required string Name { get; init; }

        /// <summary>The prefix as written, for example <c>x</c> for <c>{x:Static ...}</c>.</summary>
        public string Prefix { get; init; } = string.Empty;

        /// <summary>The arguments given without a name, in source order.</summary>
        public ImmutableList<string> PositionalArguments { get; init; } = ImmutableList<string>.Empty;

        /// <summary>The <c>Property=Value</c> arguments, parsed like any other property value.</summary>
        public ImmutableList<Property> Properties { get; init; } = ImmutableList<Property>.Empty;

        /// <summary>The extension name as written, prefix included.</summary>
        public string FullName => Prefix.IsNullOrEmpty() ? Name : $"{Prefix}:{Name}";

        public Property? this[string name] => Properties.FirstOrDefault(p => p.Name.EqualsTo(name));
    }
}
