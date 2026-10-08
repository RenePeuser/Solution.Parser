using System.Diagnostics;

namespace Solution.Parser.Project
{
    /// <summary>An entry of an old style <c>packages.config</c>.</summary>
    [DebuggerDisplay("{Id} {Version}")]
    public class Package
    {
        internal Package(string id, string version, string targetFramework)
        {
            Id = id;
            Version = version;
            TargetFramework = targetFramework;
        }

        public string Id { get; }

        public string Version { get; }

        public string TargetFramework { get; }
    }
}
