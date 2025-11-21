using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Parameter(string Type,
                            string Name,
                            ImmutableList<Attribute> Attributes,
                            string SyntaxTree,
                            bool IsOptional,
                            string? DefaultValue,
                            string FilePath) : DeclarationBase(Name, Name, SyntaxTree, FilePath);
}
