using System.Collections.Generic;
using System.Diagnostics;
using Argument.Check;

namespace SolutionParser.Nuspec
{
    [DebuggerDisplay("{" + nameof(Id) + "}")]
    public class Dependency
    {
        internal Dependency(string id, string version, IEnumerable<string> excludes)
        {
            Throw.IfNullOrWhiteSpace(() => id);
            Throw.IfNullOrWhiteSpace(() => version);
            Throw.IfNull(() => excludes);

            Id = id;
            Version = version;
            Excludes = excludes;
        }

        public string Id { get; }
        public string Version { get; }
        public IEnumerable<string> Excludes { get; }
    }
}
