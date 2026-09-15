using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A <c>Trigger</c>, <c>DataTrigger</c>, <c>MultiTrigger</c>, <c>MultiDataTrigger</c> or
    /// <c>EventTrigger</c>. Which one it is reads from <see cref="ElementBase.TypeName"/>.
    /// </summary>
    [DebuggerDisplay("{FullTypeName,nq}")]
    public record Trigger : ElementBase
    {
        public override ElementKind Kind => ElementKind.Trigger;

        /// <summary>The setters this trigger applies.</summary>
        public ImmutableList<Setter> Setters => Children.OfType<Setter>().ToImmutableList();
    }
}
