using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>A delegate type declaration.</summary>
    [DebuggerDisplay("delegate {Name}")]
    public record Delegate : DeclarationWithModifiers
    {
        public required NameSpace NameSpace { get; init; }

        public required string ReturnParameter { get; init; }

        public ImmutableList<Parameter> Parameters { get; init; } = ImmutableList<Parameter>.Empty;

        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        public bool IsGeneric => !TypeParameters.IsEmpty;
    }
}
