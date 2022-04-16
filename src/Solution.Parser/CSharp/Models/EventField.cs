using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class EventField : Event
    {
        internal EventField(string type, string name) : base(type, name)
        {
        }
    }
}
