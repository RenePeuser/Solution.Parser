using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Event : DeclarationBase
    {
        internal Event(string type, string name) : base(name)
        {
            Type = type;
        }

        public string Type { get; }
    }
}
