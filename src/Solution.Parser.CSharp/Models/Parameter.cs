using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Type} {Name}")]
    public record Parameter : DeclarationBase
    {
        public required string Type { get; init; }

        public ImmutableList<Attribute> Attributes { get; init; } = ImmutableList<Attribute>.Empty;

        /// <summary><c>ref</c>, <c>out</c>, <c>in</c>, <c>params</c>, <c>this</c> and <c>scoped</c>.</summary>
        public ImmutableList<Modifier> Modifiers { get; init; } = ImmutableList<Modifier>.Empty;

        public bool IsOptional { get; init; }

        public string? DefaultValue { get; init; }

        public bool IsParams { get; init; }

        /// <summary>True when the parameter type carries a nullable annotation of its own, as in <c>string?</c>.</summary>
        public bool IsNullable { get; init; }

        /// <summary>The position in the parameter list, zero based.</summary>
        public int Ordinal { get; init; }
    }
}
