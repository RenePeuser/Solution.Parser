using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Event : DeclarationBase
    {
        internal Event(string type, string name) : base(name)
        {
            Type = type;
        }

        public string Type { get; }
    }
}
