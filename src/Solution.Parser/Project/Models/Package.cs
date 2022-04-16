using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{PackageId.Value}")]
    public class Package
    {
        internal Package(PackageId packageId, PackageVersion packageVersion,
            PackageTargetFrameworkVersion packageTargetFrameworkVersion)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            PackageTargetFrameworkVersion = packageTargetFrameworkVersion;
        }

        public PackageId PackageId { get; }

        public PackageVersion PackageVersion { get; }

        public PackageTargetFrameworkVersion PackageTargetFrameworkVersion { get; }
    }
}
