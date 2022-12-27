using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Property(string Type,
                           string Name,
                           bool IsReadOnly,
                           IImmutableList<Modifier> Modifiers,
                           string SyntaxTree) : DeclarationWithModifiers(Name, Modifiers, SyntaxTree);
}
