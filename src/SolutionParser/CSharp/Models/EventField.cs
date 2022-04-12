using System.Diagnostics;

namespace SolutionParser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class EventField : Event
    {
        internal EventField(string type, string name) : base(type, name)
        {
        }
    }
}
