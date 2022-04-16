using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Include} : {PackageVersion.Value}")]
    public class PackageReference
    {
        internal PackageReference(string include, PackageVersion packageVersion)
        {
            Include = include;
            PackageVersion = packageVersion;
        }

        public string Include { get; }
        public PackageVersion PackageVersion { get; }
    }
}
