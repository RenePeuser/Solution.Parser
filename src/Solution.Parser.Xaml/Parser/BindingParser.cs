using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Builds a <see cref="Binding"/> from an already tokenised <c>{Binding ...}</c>. Nested arguments
    /// go back through the value parser, so <c>Converter={StaticResource Bool}</c> arrives as a
    /// <see cref="StaticResource"/>.
    /// </summary>
    internal sealed class BindingParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("Binding");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            var relativeSource = ParseOrNull(token["RelativeSource"], location, context);

            return new Binding(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                Path = context.ParseValue(Path(token), location),
                Source = ParseOrNull(token["Source"], location, context),
                Converter = ParseOrNull(token["Converter"], location, context),
                ConverterParameter = ParseOrNull(token["ConverterParameter"], location, context),
                Mode = token["Mode"] ?? "Default",
                UpdateSourceTrigger = token["UpdateSourceTrigger"] ?? "Default",
                ElementName = token["ElementName"] ?? string.Empty,
                RelativeSource = relativeSource as RelativeSource,
                FallbackValue = ParseOrNull(token["FallbackValue"], location, context),
                TargetNullValue = ParseOrNull(token["TargetNullValue"], location, context),
                StringFormat = ToStringFormat(token["StringFormat"])
            };
        }

        /// <summary>
        /// <c>{Binding Foo}</c>, <c>{Binding Path=Foo}</c> and the bare <c>{Binding}</c> all end up with
        /// a path; the bare form binds to the data context itself, which XAML writes as <c>.</c>.
        /// </summary>
        private static string Path(MarkupToken token)
        {
            var path = token.NamedOrFirstPositional("Path");

            return path.IsNullOrWhiteSpace() ? "." : path;
        }

        private static PropertyValue? ParseOrNull(string? value, XamlLocation location, XamlParseContext context)
        {
            return value is null ? null : context.ParseValue(value, location);
        }

        /// <summary>
        /// A format string reaches here already unescaped by the tokenizer, so it is wrapped rather
        /// than parsed again.
        /// </summary>
        private static StringFormat? ToStringFormat(string? value)
        {
            return value is null ? null : new StringFormat(MarkupExtensionTokenizer.Unescape(value));
        }
    }
}
