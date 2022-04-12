using System.Diagnostics;

namespace SolutionParser.Project
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
