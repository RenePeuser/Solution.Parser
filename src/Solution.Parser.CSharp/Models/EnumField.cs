using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record EnumField : DeclarationBase
    {
        public ImmutableList<Attribute> Attributes { get; init; } = ImmutableList<Attribute>.Empty;

        public DocumentationComment Documentation { get; init; } = DocumentationComment.None;

        /// <summary>The explicitly assigned value, for example <c>1</c> in <c>A = 1</c>, otherwise null.</summary>
        public string? Value { get; init; }

        public bool HasExplicitValue => Value is not null;
    }
}
