using System.Collections.Immutable;
using System.Diagnostics;
using Argument.Check;

namespace Solution.Parser.Nuspec
{
    [DebuggerDisplay("{Id}")]
    public class Dependency
    {
        internal Dependency(string id, string version, ImmutableList<string> excludes)
        {
            Throw.IfNullOrWhiteSpace(id);
            Throw.IfNullOrWhiteSpace(version);
            Throw.IfNull(excludes);

            Id = id;
            Version = version;
            Excludes = excludes;
        }

        public string Id { get; }
        public string Version { get; }
        public ImmutableList<string> Excludes { get; }
    }
}
