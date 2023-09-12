using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record EnumField(string Name, string SyntaxTree, string FilePath) : DeclarationBase(Name, Name, SyntaxTree, FilePath);
}
