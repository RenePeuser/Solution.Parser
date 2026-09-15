using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>A document element the parser has no special shape for, for example a bare <c>&lt;Grid&gt;</c>.</summary>
    [DebuggerDisplay("{FullTypeName,nq}")]
    public record ControlRoot : Root
    {
        public override RootKind RootKind => RootKind.Control;
    }
}
