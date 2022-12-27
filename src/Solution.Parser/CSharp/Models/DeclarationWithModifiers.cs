using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record DeclarationWithModifiers(string Name, IImmutableList<Modifier> Modifiers, string SyntaxTree) : DeclarationBase(Name, SyntaxTree);
}
