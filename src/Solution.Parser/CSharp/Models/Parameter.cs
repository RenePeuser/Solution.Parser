using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Parameter(string Type, string Name, IImmutableList<Attribute> Attributes, string SyntaxTree) : DeclarationBase(Name, SyntaxTree);
}
