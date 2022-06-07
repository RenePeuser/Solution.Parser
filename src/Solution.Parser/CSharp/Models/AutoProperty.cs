using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record AutoProperty : Property
    {
        internal AutoProperty(string type, string name, bool isReadOnly, IImmutableList<Modifier> modifiers) : base(
            type, name, isReadOnly, modifiers)
        {
        }
    }
}
