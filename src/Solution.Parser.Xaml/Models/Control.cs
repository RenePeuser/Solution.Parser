using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>Any element the parser has no special shape for, which is most of them.</summary>
    [DebuggerDisplay("{FullTypeName,nq} Name:{XName} Key:{XKey}")]
    public record Control : ElementBase
    {
        public override ElementKind Kind => ElementKind.Control;
    }
}
