using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record TypeParameter(string Name,
                                VarianceKind Variance,
                                ImmutableList<string> Constraints,
                                ImmutableList<Attribute> Attributes);

    public enum VarianceKind
    {
        None,

        In,

        Out
    }
}
