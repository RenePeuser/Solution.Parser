using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FullyQualifiedName}")]
    public record UserControl : Root
    {
        public override RootKind RootKind => RootKind.UserControl;
    }
}
