using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record EnumField : DeclarationBase
    {
        internal EnumField(string name)
            : base(name)
        {
        }
    }
}
