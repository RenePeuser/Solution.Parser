using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>An event declared with explicit <c>add</c> and <c>remove</c> accessors.</summary>
    [DebuggerDisplay("{Type} {Name}")]
    public record Event : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        public string? ExplicitInterfaceSpecifier { get; init; }

        public bool HasAdd { get; init; }

        public bool HasRemove { get; init; }
    }
}
