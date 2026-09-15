namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The last parser in the list. A custom or unknown extension keeps its name and its arguments
    /// instead of being reduced to raw text, so a rule can still read it.
    /// </summary>
    internal sealed class GenericMarkupExtensionParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return true;
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            return new MarkupExtension(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context)
            };
        }
    }
}
