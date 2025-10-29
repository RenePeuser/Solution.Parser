using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record DeclarationBase(string Name,
                                  string FullQualifiedName,
                                  string SyntaxTree,
                                  string FilePath);
}
