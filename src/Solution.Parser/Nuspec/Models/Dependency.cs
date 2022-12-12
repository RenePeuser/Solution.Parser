using System.Collections.Immutable;
using System.Diagnostics;
using Argument.Check;

namespace Solution.Parser.Nuspec
{
    [DebuggerDisplay("{Id}")]
    public class Dependency
    {
        internal Dependency(string id, string version, IImmutableList<string> excludes)
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
        public IImmutableList<string> Excludes { get; }
    }
}
