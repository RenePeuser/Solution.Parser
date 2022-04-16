using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class NameSpace
    {
        internal NameSpace(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
