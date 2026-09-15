namespace Solution.Parser.Xaml
{
    public static class XamlParseExtensions
    {
        public static XamlSyntaxTree Parse(this IXamlFileInfo xamlFileInfo)
        {
            return new XamlParser().Parse(xamlFileInfo);
        }

        public static XamlSyntaxTree Parse(this IXamlFileInfo xamlFileInfo, string xamlContent)
        {
            return new XamlParser().Parse(xamlFileInfo, xamlContent);
        }

        /// <summary>
        /// Parses markup that is not on disk, which is what a test or an in memory rule needs:
        /// <see cref="XamlFileInfo"/> insists the file exists even when the content is passed in.
        /// </summary>
        public static XamlSyntaxTree ParseXaml(this string xamlContent, string filePath)
        {
            return new XamlParser().ParseContent(xamlContent, filePath);
        }
    }
}
