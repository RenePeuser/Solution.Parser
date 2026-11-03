using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Type} {Name}")]
    public record Parameter : DeclarationBase
    {
        public required string Type { get; init; }

        /// <summary>The type taken apart, see <see cref="CSharp.TypeReference"/>; <see cref="Type"/> is the same as text.</summary>
        public TypeReference TypeReference { get; init; } = TypeReference.None;

        public ImmutableList<Attribute> Attributes { get; init; } = ImmutableList<Attribute>.Empty;

        /// <summary><c>ref</c>, <c>out</c>, <c>in</c>, <c>params</c>, <c>this</c> and <c>scoped</c>.</summary>
        public ImmutableList<Modifier> Modifiers { get; init; } = ImmutableList<Modifier>.Empty;

        public bool IsOptional { get; init; }

        public string? DefaultValue { get; init; }

        /// <summary>The default value classified, see <see cref="Expression"/>; <see cref="DefaultValue"/> is the same as text.</summary>
        public Expression? DefaultValueExpression { get; init; }

        public bool IsParams { get; init; }

        /// <summary>True when the parameter type carries a nullable annotation of its own, as in <c>string?</c>.</summary>
        public bool IsNullable { get; init; }

        /// <summary>The position in the parameter list, zero based.</summary>
        public int Ordinal { get; init; }

        /// <summary>True for <c>this string value</c>, the receiver of a classic extension method.</summary>
        public bool IsThis => Modifiers.Contains(Modifier.This);

        /// <summary>Passed by <c>ref</c>, including <c>ref readonly</c>.</summary>
        public bool IsRef => Modifiers.Contains(Modifier.Ref);

        /// <summary>Passed as <c>ref readonly</c>.</summary>
        public bool IsRefReadOnly => IsRef && Modifiers.Contains(Modifier.ReadOnly);

        public bool IsOut => Modifiers.Contains(Modifier.Out);

        public bool IsIn => Modifiers.Contains(Modifier.In);

        public bool IsScoped => Modifiers.Contains(Modifier.Scoped);
    }
}
