using System.Diagnostics;

namespace SolutionParser.CSharp
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
