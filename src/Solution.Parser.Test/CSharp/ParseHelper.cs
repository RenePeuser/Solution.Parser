using Solution.Parser.CSharp;

namespace Solution.Parser.Test.CSharp
{
    internal static class ParseHelper
    {
        internal const string FilePath = @"C:\repo\Sample.cs";

        internal static CSharpSyntaxTree ParseCode(string code)
        {
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);

            return syntaxTree.Parse(FilePath);
        }
    }
}
