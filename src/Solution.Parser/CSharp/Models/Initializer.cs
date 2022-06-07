using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Value}")]
    public class Initializer : ImmutableSemanticType<string>
    {
        internal Initializer(string value) : base(value)
        {
        }
    }
}
