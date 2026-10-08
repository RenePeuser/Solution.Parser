using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FullyQualifiedName}")]
    public record Page : Root
    {
        public override RootKind RootKind => RootKind.Page;
    }
}
