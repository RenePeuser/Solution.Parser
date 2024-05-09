namespace Solution.Parser.CSharp
{
    public static class StringExtensions
    {
        private static readonly CodeFormatter CodeFormatter = new();

        public static string FormatSyntaxTree(this string value)
        {
            return CodeFormatter.FormatCode(value);
        }
    }
}
