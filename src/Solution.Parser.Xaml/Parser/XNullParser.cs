using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Matches <c>{x:Null}</c> by name. The old predicate matched any value merely containing the text
    /// <c>x:Null</c>, so a converter parameter mentioning it turned into a null value.
    /// </summary>
    internal sealed class XNullParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("Null");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            return new NullExtension(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context)
            };
        }
    }
}
