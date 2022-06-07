using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Parameter(string Type, string Name) : DeclarationBase(Name);
}
