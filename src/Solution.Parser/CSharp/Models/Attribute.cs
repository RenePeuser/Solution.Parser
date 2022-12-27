using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Attribute(string Name, IImmutableList<string> Arguments, string SyntaxTree) : DeclarationBase(Name, SyntaxTree);
}
