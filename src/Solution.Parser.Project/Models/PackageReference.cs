using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Include} : {Version}")]
    public class PackageReference
    {
        internal PackageReference(string include, string version)
        {
            Include = include;
            Version = version;
        }

        public string Include { get; }

        public string Version { get; }
    }
}
