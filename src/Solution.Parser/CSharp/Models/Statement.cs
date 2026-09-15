using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>A top level statement of the file.</summary>
    [DebuggerDisplay("{SyntaxTree}")]
    public record Statement(string SyntaxTree, string FilePath, CodeLocation Location);
}
