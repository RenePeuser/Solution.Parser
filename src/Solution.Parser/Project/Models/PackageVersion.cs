using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Value}")]
    public class PackageVersion : ImmutableSemanticType<string>
    {
        internal PackageVersion(string value)
            : base(value)
        {
        }
    }
}
