using System.Collections.Immutable;
using System.Diagnostics;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    [DebuggerDisplay("{Name}")]
    public record ControllerInfo
    {
        public required string Name { get; init; } = string.Empty;

        public required string DomainName { get; init; } = string.Empty;

        public required VersionInfo Version { get; init; } = new VersionInfo("1.0", "V1");

        public required IImmutableList<string> BaseUrls { get; init; } = ImmutableList<string>.Empty;

        public required IImmutableList<MethodInfos> Methods { get; init; } = ImmutableList<MethodInfos>.Empty;

        public IImmutableList<Attribute> Attributes { get; set; } = ImmutableList<Attribute>.Empty;
    }
}
