using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Property(string Type,
                           string Name,
                           bool IsReadOnly,
                           bool IsRequired,
                           bool IsNullable,
                           ImmutableList<Modifier> Modifiers,
                           ImmutableList<Attribute> Attributes,
                           string SyntaxTree,
                           string FullQualifiedName,
                           string FilePath) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers,
                                                                       Attributes, SyntaxTree, FilePath);
}
