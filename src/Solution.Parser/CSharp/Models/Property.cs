using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public record Property : DeclarationWithModifiers
    {
        internal Property(string type, string name, bool isReadOnly, IImmutableList<Modifier> modifiers) : base(name,
            modifiers)
        {
            Type = type;
            IsReadOnly = isReadOnly;
        }

        public string Type { get; }

        public bool IsReadOnly { get; }
    }
}
