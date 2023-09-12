using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record DeclarationWithModifiers(string Name, string FullQualifiedName, IImmutableList<Modifier> Modifiers, string SyntaxTree, string FilePath) : DeclarationBase(Name, FullQualifiedName, SyntaxTree, FilePath);
}
