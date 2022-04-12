using System.Diagnostics;

namespace SolutionParser.Project
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
