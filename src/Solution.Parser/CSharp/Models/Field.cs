using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public record Field : DeclarationBase
    {
        internal Field(string name, string type, IImmutableList<Modifier> modifiers, Initializer initializer) :
            base(name)
        {
            Type = type;
            Modifiers = modifiers;
            Initializer = initializer;
        }

        public string Type { get; }

        public IImmutableList<Modifier> Modifiers { get; }

        public Initializer Initializer { get; }
    }
}
