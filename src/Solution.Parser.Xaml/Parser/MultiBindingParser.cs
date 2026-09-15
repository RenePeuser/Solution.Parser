using System.Collections.Immutable;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    internal sealed class MultiBindingParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("MultiBinding");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            return new MultiBinding(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                Converter = ParseOrNull(token["Converter"], location, context),
                ConverterParameter = ParseOrNull(token["ConverterParameter"], location, context),
                Mode = token["Mode"] ?? "Default",
                StringFormat = token["StringFormat"] is { } format
                    ? new StringFormat(MarkupExtensionTokenizer.Unescape(format))
                    : null,
                Bindings = InlineBindings(token, location, context)
            };
        }

        private static ImmutableList<Binding> InlineBindings(MarkupToken token,
                                                             XamlLocation location,
                                                             XamlParseContext context)
        {
            return token.PositionalArguments
                        .Select(a => context.ParseValue(a, location))
                        .OfType<Binding>()
                        .ToImmutableList();
        }

        private static PropertyValue? ParseOrNull(string? value, XamlLocation location, XamlParseContext context)
        {
            return value is null ? null : context.ParseValue(value, location);
        }
    }
}
