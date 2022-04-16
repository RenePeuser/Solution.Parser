using System.Collections.Generic;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Field : DeclarationBase
    {
        internal Field(string name, string type, IEnumerable<Modifier> modifiers, Initializer initializer) :
            base(name)
        {
            Type = type;
            Modifiers = modifiers;
            Initializer = initializer;
        }

        public string Type { get; }

        public IEnumerable<Modifier> Modifiers { get; }

        public Initializer Initializer { get; }
    }
}
