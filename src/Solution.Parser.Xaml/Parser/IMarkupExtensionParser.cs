namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Turns one flavour of markup extension into its model. Selection happens on the tokenised
    /// extension name, not on a substring of the raw text, so an extension is only ever handled by the
    /// parser that actually understands it.
    /// </summary>
    internal interface IMarkupExtensionParser
    {
        bool IsThisTheCorrectParserFor(MarkupToken token);

        PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context);
    }
}
