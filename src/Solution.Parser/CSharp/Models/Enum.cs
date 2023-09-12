using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Enum(NameSpace NameSpace,
                       string Name,
                       IImmutableList<Modifier> Modifiers,
                       IImmutableList<EnumField> EnumFields,
                       string FullQualifiedName,
                       string SyntaxTree,
                       string FilePath) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers, SyntaxTree, FilePath);
}
