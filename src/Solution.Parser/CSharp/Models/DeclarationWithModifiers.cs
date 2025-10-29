using System.Collections.Immutable;

namespace Solution.Parser.CSharp
{
    public record DeclarationWithModifiers(string Name,
                                           string FullQualifiedName,
                                           IImmutableList<Modifier> Modifiers,
                                           IImmutableList<Attribute> Attributes,
                                           string SyntaxTree,
                                           string FilePath) : DeclarationBase(Name, FullQualifiedName, SyntaxTree,
                                                                              FilePath);
}
