using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>A delegate type declaration.</summary>
    [DebuggerDisplay("delegate {Name}")]
    public record Delegate : DeclarationWithModifiers
    {
        public required Namespace Namespace { get; init; }

        public required string ReturnParameter { get; init; }

        /// <summary>The return type taken apart, see <see cref="TypeReference"/>; <see cref="ReturnParameter"/> is the same as text.</summary>
        public TypeReference ReturnType { get; init; } = TypeReference.None;

        public ImmutableList<Parameter> Parameters { get; init; } = ImmutableList<Parameter>.Empty;

        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        public bool IsGeneric => !TypeParameters.IsEmpty;
    }
}
