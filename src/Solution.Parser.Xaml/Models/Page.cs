using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FullQualifiedName}")]
    public record Page : Root
    {
        public override RootKind RootKind => RootKind.Page;
    }
}
