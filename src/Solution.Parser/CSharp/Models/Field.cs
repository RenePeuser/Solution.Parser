using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Field(string Name,
                        string Type,
                        IImmutableList<Modifier> Modifiers,
                        Initializer? Initializer,
                        string SyntaxTree) : DeclarationBase(Name, Name, SyntaxTree);
}
