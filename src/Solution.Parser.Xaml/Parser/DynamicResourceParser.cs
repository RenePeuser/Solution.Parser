using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    internal sealed class DynamicResourceParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("DynamicResource");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            return new DynamicResource(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                ResourceKey = token.ToResourceKey(location, context)
            };
        }
    }
}
