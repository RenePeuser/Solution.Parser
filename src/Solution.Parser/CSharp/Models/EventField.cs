using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public record EventField : Event
    {
        internal EventField(string type, string name) : base(type, name)
        {
        }
    }
}
