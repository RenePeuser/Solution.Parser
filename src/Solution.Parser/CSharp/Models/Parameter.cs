using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public record Parameter : DeclarationBase
    {
        internal Parameter(string type, string name) : base(name)
        {
            Type = type;
        }

        public string Type { get; }
    }
}
