using System.Diagnostics;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Something set on an element. Plain attributes land here; <see cref="AttachedProperty"/> and
    /// <see cref="PropertyElement"/> are the two shapes that carry an owning type as well.
    /// </summary>
    [DebuggerDisplay("{FullName,nq} = {PropertyValue}")]
    public record Property
    {
        /// <summary>The property name without its prefix, for example <c>Name</c> for <c>x:Name</c>.</summary>
        public required string Name { get; init; }

        public required XamlLocation Location { get; init; }

        /// <summary>The prefix as written in the file, for example <c>x</c> for <c>x:Name</c>.</summary>
        public string Prefix { get; init; } = string.Empty;

        /// <summary>The XML namespace URI the property name resolves to, empty for an unprefixed attribute.</summary>
        public string XmlNamespace { get; init; } = string.Empty;

        public PropertyValue? PropertyValue { get; init; }

        /// <summary>
        /// True for a XAML language directive such as <c>x:Name</c>, <c>x:Key</c> or <c>x:Class</c>.
        /// Without this a directive is indistinguishable from a property that happens to share its name.
        /// </summary>
        public bool IsXamlDirective => XmlNamespace.EqualsTo(XamlNamespaces.Xaml);

        /// <summary>The property name as written, prefix included.</summary>
        public string FullName => Prefix.IsNullOrEmpty() ? Name : $"{Prefix}:{Name}";
    }
}
