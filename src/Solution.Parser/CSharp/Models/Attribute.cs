using System.Collections.Generic;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Attribute : DeclarationBase
    {
        internal Attribute(string name, IEnumerable<string> arguments) : base(name)
        {
            Arguments = arguments;
        }

        public IEnumerable<string> Arguments { get; }
    }
}
