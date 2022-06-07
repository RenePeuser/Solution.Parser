using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record EventField(string Type, string Name) : Event(Type, Name);
}
