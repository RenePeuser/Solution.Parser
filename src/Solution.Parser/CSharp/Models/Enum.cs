using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Enum(NameSpace NameSpace,
                       string Name,
                       ImmutableList<Modifier> Modifiers,
                       ImmutableList<EnumField> EnumFields,
                       ImmutableList<Attribute> Attributes,
                       string FullQualifiedName,
                       string SyntaxTree,
                       string FilePath) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers, Attributes, SyntaxTree, FilePath);
}
