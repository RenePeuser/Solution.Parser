namespace Solution.Parser.CSharp
{
    [System.Obsolete("A leftover of a code generator, unrelated to parsing; it will be removed in the next major version.")]
    public static class DeclarationBaseExtensions
    {
        private static readonly CodeFormatter CodeFormatter = new();

        public static string FormatSyntaxTree(this DeclarationBase declarationBase)
        {
            return CodeFormatter.FormatCode(declarationBase.SourceCode);
        }
    }
}
