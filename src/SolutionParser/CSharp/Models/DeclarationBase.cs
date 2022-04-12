using System.Diagnostics;
using Argument.Check;

namespace SolutionParser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class DeclarationBase
    {
        internal DeclarationBase(string name)
        {
            Throw.IfNullOrWhiteSpace(() => name);

            Name = name;
        }

        public string Name { get; }
    }
}
