namespace Solution.Parser.CSharp
{
    [System.Obsolete("A leftover of a code generator, unrelated to parsing; it will be removed in the next major version.")]
    public static class StringExtensions
    {
        private static readonly CodeFormatter CodeFormatter = new();

        public static string FormatSyntaxTree(this string value)
        {
            return CodeFormatter.FormatCode(value);
        }
    }
}
