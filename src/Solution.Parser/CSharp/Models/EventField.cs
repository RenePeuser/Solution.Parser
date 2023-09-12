using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record EventField(string Type, string Name, string SyntaxTree, string FilePath) : Event(Type, Name, SyntaxTree, FilePath);
}
