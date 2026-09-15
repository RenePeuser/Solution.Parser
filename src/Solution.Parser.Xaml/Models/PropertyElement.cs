using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A property written as an element, such as <c>&lt;Grid.RowDefinitions&gt;</c> or
    /// <c>&lt;UserControl.Resources&gt;</c>. XAML treats these as properties of the surrounding
    /// element, not as children of it, which is why they live in
    /// <see cref="ElementBase.Properties"/> rather than <see cref="ElementBase.Children"/>.
    /// </summary>
    [DebuggerDisplay("{FullQualifiedName,nq} ({Children.Count} children)")]
    public record PropertyElement : Property
    {
        /// <summary>The type declaring the property, for example <c>Grid</c> for <c>&lt;Grid.RowDefinitions&gt;</c>.</summary>
        public required string OwnerTypeName { get; init; }

        /// <summary>The elements written inside the property element.</summary>
        public ImmutableList<ElementBase> Children { get; init; } = ImmutableList<ElementBase>.Empty;

        /// <summary><c>Owner.Name</c>, exactly as the element is written without its prefix.</summary>
        public string FullQualifiedName => $"{OwnerTypeName}.{Name}";
    }
}
