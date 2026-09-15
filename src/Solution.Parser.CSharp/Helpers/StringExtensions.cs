namespace Solution.Parser.CSharp
{
    public static class DeclarationBaseExtensions
    {
        private static readonly CodeFormatter CodeFormatter = new();

        public static string FormatSyntaxTree(this DeclarationBase declarationBase)
        {
            return CodeFormatter.FormatCode(declarationBase.SyntaxTree);
        }
    }
}
