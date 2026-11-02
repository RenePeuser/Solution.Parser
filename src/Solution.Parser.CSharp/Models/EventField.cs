using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>A field like event, declared as <c>public event EventHandler Changed;</c>.</summary>
    [DebuggerDisplay("{Type} {Name}")]
    public record EventField : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        /// <summary>The type taken apart, see <see cref="CSharp.TypeReference"/>; <see cref="Type"/> is the same as text.</summary>
        public TypeReference TypeReference { get; init; } = TypeReference.None;

        public string? Initializer { get; init; }

        /// <summary>See <see cref="Field.IsPartOfMultiVariableDeclaration"/>.</summary>
        public bool IsPartOfMultiVariableDeclaration { get; init; }
    }
}
