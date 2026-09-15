using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A using directive. <c>Value</c> stays the namespace or type
    /// being imported, so existing rules keep working; the flags describe how it was written.
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public record Using(string Value) : ImmutableSemanticType<string>(Value)
    {
        /// <summary>The alias in <c>using Foo = System.Bar;</c>, otherwise null.</summary>
        public string? Alias { get; init; }

        /// <summary>True for <c>global using</c>.</summary>
        public bool IsGlobal { get; init; }

        /// <summary>True for <c>using static</c>.</summary>
        public bool IsStatic { get; init; }

        public bool IsAlias => Alias is not null;

        public CodeLocation Location { get; init; } = CodeLocation.None;
    }
}
