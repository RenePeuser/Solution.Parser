namespace Solution.Parser.XAML
{
    public static class XamlParseExtensions
    {
        public static XamlSyntaxTree Parse(this IXamlFileInfo xamlFileInfo)
        {
            var xamlParser = new XamlParser();
            var result = xamlParser.Parse(xamlFileInfo);
            return result;
        }

        public static XamlSyntaxTree Parse(this IXamlFileInfo xamlFileInfo, string xamlContent)
        {
            var xamlParser = new XamlParser();
            var result = xamlParser.Parse(xamlFileInfo, xamlContent);
            return result;
        }
    }
}
