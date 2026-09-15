using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Builds an <see cref="XStaticMarkupExtension"/>. The old parser returned an
    /// <see cref="XTypeMarkupExtension"/> here, so no caller could ever see the static member.
    /// </summary>
    internal sealed class XStaticParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("Static");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            var member = token.NamedOrFirstPositional("Member").Split(':').Last();
            var separator = member.LastIndexOf('.');

            return new XStaticMarkupExtension(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                Type = separator < 0 ? string.Empty : member[..separator],
                Member = separator < 0 ? member : member[(separator + 1)..]
            };
        }
    }
}
