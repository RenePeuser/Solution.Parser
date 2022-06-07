using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record EnumField(string Name) : DeclarationBase(Name);
}
