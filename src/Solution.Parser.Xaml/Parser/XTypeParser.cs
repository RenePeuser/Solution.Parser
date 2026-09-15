using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    internal sealed class XTypeParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("Type");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            return new XTypeMarkupExtension(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                Type = token.NamedOrFirstPositional("TypeName").Split(':').Last()
            };
        }
    }
}
