using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FullyQualifiedName}")]
    public record Window : Root
    {
        public override RootKind RootKind => RootKind.Window;
    }
}
