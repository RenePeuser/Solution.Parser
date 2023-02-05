using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record EnumField(string Name, string SyntaxTree) : DeclarationBase(Name, Name, SyntaxTree);
}
