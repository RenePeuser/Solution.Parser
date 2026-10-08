using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>A top level statement of the file.</summary>
    [DebuggerDisplay("{SourceCode}")]
    public record Statement(string SourceCode, string FilePath, CodeLocation Location);
}
