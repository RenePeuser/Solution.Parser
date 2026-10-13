namespace Solution.Parser.Xaml
{
    public static class XamlParseExtensions
    {
        /// <summary>
        /// Parses markup that is not on disk, which is what a test or an in memory rule needs.
        /// <paramref name="filePath"/> does not have to exist.
        /// </summary>
        public static XamlSyntaxTree ParseXaml(this string xamlContent, string filePath)
        {
            return new XamlParser().ParseContent(xamlContent, filePath);
        }
    }
}
