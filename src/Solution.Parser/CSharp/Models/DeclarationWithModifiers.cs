using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public abstract record DeclarationWithModifiers : DeclarationBase
    {
        public ImmutableList<Modifier> Modifiers { get; init; } = ImmutableList<Modifier>.Empty;

        public ImmutableList<Attribute> Attributes { get; init; } = ImmutableList<Attribute>.Empty;

        /// <summary>
        /// The effective accessibility, including the language default when no modifier is written.
        /// </summary>
        public Accessibility Accessibility { get; init; } = Accessibility.NotApplicable;

        public DocumentationComment Documentation { get; init; } = DocumentationComment.None;
    }
}
