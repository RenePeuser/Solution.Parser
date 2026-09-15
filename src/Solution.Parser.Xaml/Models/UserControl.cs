using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FullQualifiedName}")]
    public record UserControl : Root
    {
        public override RootKind RootKind => RootKind.UserControl;
    }
}
