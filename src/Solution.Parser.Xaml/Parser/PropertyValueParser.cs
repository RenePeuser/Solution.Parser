using System.Collections.Immutable;
using System.Linq;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Turns an attribute value into a <see cref="PropertyValue"/>.
    /// </summary>
    /// <remarks>
    /// A value is markup if and only if it starts with an unescaped <c>{</c>. Everything else is text.
    /// The old parser decided by substring, so <c>ToolTip="Note: press F1"</c> was taken for a markup
    /// extension and took the whole file down with it.
    /// </remarks>
    internal sealed class PropertyValueParser
    {
        private static readonly ImmutableList<IMarkupExtensionParser> MarkupParsers =
            ImmutableList.Create<IMarkupExtensionParser>(new BindingParser(),
                                                         new MultiBindingParser(),
                                                         new StaticResourceParser(),
                                                         new DynamicResourceParser(),
                                                         new TemplateBindingParser(),
                                                         new RelativeSourceParser(),
                                                         new XTypeParser(),
                                                         new XStaticParser(),
                                                         new XNullParser(),
                                                         // Anything else keeps its name and arguments
                                                         // rather than being thrown away.
                                                         new GenericMarkupExtensionParser());

        internal PropertyValue Parse(string? value, XamlLocation location, XamlParseContext context)
        {
            if (value is null)
            {
                return new PropertyValue(string.Empty);
            }

            if (MarkupExtensionTokenizer.IsEscapedLiteral(value))
            {
                var literal = MarkupExtensionTokenizer.Unescape(value);

                return IsCompositeFormat(literal) ? new StringFormat(literal) : new PropertyValue(literal);
            }

            if (!MarkupExtensionTokenizer.IsMarkupExtension(value))
            {
                return ParseLiteral(value);
            }

            if (!MarkupExtensionTokenizer.TryTokenize(value, out var token))
            {
                context.Report(XamlDiagnostic.MalformedMarkupExtension,
                               $"Could not read the markup extension '{value}'. Check that its braces and quotes are balanced.",
                               location);

                return new UnknownPropertyValue(value);
            }

            var parser = MarkupParsers.First(p => p.IsThisTheCorrectParserFor(token));

            return parser.Parse(token, location, context);
        }

        /// <summary>
        /// Plain text, except for the <c>(Owner.Property)</c> form that names an attached property
        /// inside a binding path.
        /// </summary>
        private static PropertyValue ParseLiteral(string value)
        {
            var trimmed = value.Trim();

            if (!trimmed.StartsWith('(') || !trimmed.EndsWith(')'))
            {
                return IsCompositeFormat(value) ? new StringFormat(value) : new PropertyValue(value);
            }

            var inner = trimmed[1..^1].Split(':').Last();
            var separator = inner.LastIndexOf('.');

            if (separator <= 0 || separator.Equals(inner.Length - 1))
            {
                return new PropertyValue(value);
            }

            return new AttachedPropertyValue(value)
            {
                OwnerTypeName = inner[..separator],
                PropertyName = inner[(separator + 1)..]
            };
        }

        /// <summary>True for text carrying a composite format item such as <c>{0:N2}</c>.</summary>
        private static bool IsCompositeFormat(string value)
        {
            var open = value.IndexOf('{');

            return open >= 0 && open + 1 < value.Length && char.IsDigit(value[open + 1]) && value.IndexOf('}', open) > open;
        }
    }
}
