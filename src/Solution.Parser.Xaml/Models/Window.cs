using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FullQualifiedName}")]
    public record Window : Root
    {
        public override RootKind RootKind => RootKind.Window;
    }
}
