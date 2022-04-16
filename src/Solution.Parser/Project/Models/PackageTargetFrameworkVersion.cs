using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class PackageTargetFrameworkVersion : ImmutableSemanticType<string>
    {
        internal PackageTargetFrameworkVersion(string value)
            : base(value)
        {
        }
    }
}
