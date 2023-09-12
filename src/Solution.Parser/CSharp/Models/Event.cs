using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Event(string Type, string Name, string SyntaxTree, string FilePath) : DeclarationBase(Name, Name, SyntaxTree, FilePath);
}
