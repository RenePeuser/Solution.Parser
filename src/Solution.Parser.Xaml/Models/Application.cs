using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>An <c>App.xaml</c>, whose document element is <c>&lt;Application&gt;</c>.</summary>
    [DebuggerDisplay("{FullQualifiedName}")]
    public record Application : Root
    {
        public override RootKind RootKind => RootKind.Application;
    }
}
