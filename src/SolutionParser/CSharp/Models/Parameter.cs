using System.Diagnostics;

namespace SolutionParser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Parameter : DeclarationBase
    {
        internal Parameter(string type, string name) : base(name)
        {
            Type = type;
        }

        public string Type { get; }
    }
}
