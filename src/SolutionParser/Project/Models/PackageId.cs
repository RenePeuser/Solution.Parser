using System.Diagnostics;

namespace SolutionParser.Project
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class PackageId : ImmutableSemanticType<string>
    {
        internal PackageId(string value)
            : base(value)
        {
        }
    }
}
