using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public class EnumField : DeclarationBase
    {
        internal EnumField(string name)
            : base(name)
        {
        }
    }
}
