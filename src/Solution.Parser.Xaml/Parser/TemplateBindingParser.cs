using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    internal sealed class TemplateBindingParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("TemplateBinding");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            return new TemplateBinding(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                PropertyName = token.NamedOrFirstPositional("Property").Split(':').Last()
            };
        }
    }
}
