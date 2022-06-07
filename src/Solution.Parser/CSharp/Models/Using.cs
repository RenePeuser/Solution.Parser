using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Value}")]
    public record Using(string Value) : ImmutableSemanticType<string>(Value);
}
