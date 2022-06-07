using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Value}")]
    public class Using : ImmutableSemanticType<string>
    {
        internal Using(string value) : base(value)
        {
        }
    }
}
