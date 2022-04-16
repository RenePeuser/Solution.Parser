using System.Diagnostics;

namespace Solution.Parser.Project
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
