using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class PackageVersion : ImmutableSemanticType<string>
    {
        internal PackageVersion(string value)
            : base(value)
        {
        }
    }
}
