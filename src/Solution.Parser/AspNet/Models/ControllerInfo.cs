using System.Collections.Immutable;
using System.Diagnostics;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    [DebuggerDisplay("{Name}")]
    public record ControllerInfo
    {
        public required string Name { get; init; } = string.Empty;

        public required string FilePath { get; init; } = string.Empty;

        public required string DomainName { get; init; } = string.Empty;

        public required VersionInfo Version { get; init; } = new("1.0", "V1");

        public required ImmutableList<string> BaseUrls { get; init; } = ImmutableList<string>.Empty;

        public required ImmutableList<MethodInfos> Methods { get; init; } = ImmutableList<MethodInfos>.Empty;

        public ImmutableList<Attribute> Attributes { get; init; } = ImmutableList<Attribute>.Empty;


    }
}
