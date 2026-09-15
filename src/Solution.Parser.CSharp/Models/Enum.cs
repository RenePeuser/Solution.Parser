using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Enum : TypeDeclaration
    {
        public override TypeKind Kind => TypeKind.Enum;

        public ImmutableList<EnumField> EnumFields { get; init; } = ImmutableList<EnumField>.Empty;

        /// <summary>The declared underlying type, for example <c>byte</c> in <c>enum E : byte</c>.</summary>
        public string? UnderlyingType { get; init; }

        public bool IsFlags => Attributes.Any(a => a.IsNamed("Flags"));
    }
}
