using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>An event declared with explicit <c>add</c> and <c>remove</c> accessors.</summary>
    [DebuggerDisplay("{Type} {Name}")]
    public record Event : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        /// <summary>The type taken apart, see <see cref="CSharp.TypeReference"/>; <see cref="Type"/> is the same as text.</summary>
        public TypeReference TypeReference { get; init; } = TypeReference.None;

        public string? ExplicitInterfaceSpecifier { get; init; }

        public bool HasAdd { get; init; }

        public bool HasRemove { get; init; }

        /// <summary>The <c>add</c> accessor with its body; null when there is none.</summary>
        public Accessor? Add { get; init; }

        /// <summary>The <c>remove</c> accessor with its body; null when there is none.</summary>
        public Accessor? Remove { get; init; }
    }
}
