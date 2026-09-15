using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Builds a <see cref="RelativeSource"/>. The old parser took the last space separated word, so
    /// <c>{RelativeSource AncestorType=Window, Mode=FindAncestor}</c> reported its mode as
    /// <c>Mode=FindAncestor}</c>.
    /// </summary>
    internal sealed class RelativeSourceParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("RelativeSource");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            var ancestorType = (token["AncestorType"] ?? string.Empty).ToTypeName(location, context);

            return new RelativeSource(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                Mode = Mode(token, ancestorType),
                AncestorType = ancestorType,
                AncestorLevel = AncestorLevel(token)
            };
        }

        /// <summary>
        /// The mode comes from <c>Mode=</c> or from the positional argument. Naming an ancestor type
        /// without a mode implies <c>FindAncestor</c>, which is how WPF reads it too.
        /// </summary>
        private static string Mode(MarkupToken token, string ancestorType)
        {
            var mode = token.NamedOrFirstPositional("Mode");

            if (!mode.IsNullOrWhiteSpace())
            {
                return mode;
            }

            return ancestorType.IsNullOrWhiteSpace() ? string.Empty : "FindAncestor";
        }

        private static int AncestorLevel(MarkupToken token)
        {
            return int.TryParse(token["AncestorLevel"], out var level) ? level : 1;
        }
    }
}
