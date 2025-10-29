using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record AutoProperty(string Type,
                               string Name,
                               bool IsReadOnly,
                               IImmutableList<Modifier> Modifiers,
                               IImmutableList<Attribute> Attributes,
                               string SyntaxTree,
                               string FullQualifiedName,
                               string filePath) : Property(Type, Name, IsReadOnly, Modifiers, Attributes, SyntaxTree, FullQualifiedName, filePath);
}
