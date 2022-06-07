using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Value}")]
    public record Initializer(string Value) : ImmutableSemanticType<string>(Value);
}
