using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// An attached property such as <c>Grid.Row="1"</c>. <see cref="Property.Name"/> is the property
    /// name on its own, so a lookup by name behaves like it does for any other property, and
    /// <see cref="FullQualifiedName"/> is what <c>element["Grid.Row"]</c> matches.
    /// </summary>
    [DebuggerDisplay("{FullQualifiedName,nq} = {PropertyValue}")]
    public record AttachedProperty : Property
    {
        /// <summary>The type declaring the property, for example <c>Grid</c> for <c>Grid.Row</c>.</summary>
        public required string OwnerTypeName { get; init; }

        /// <summary><c>Owner.Name</c>, exactly as the attribute is written without its prefix.</summary>
        public string FullQualifiedName => $"{OwnerTypeName}.{Name}";

        /// <summary>The name of the backing dependency property field, for example <c>RowProperty</c>.</summary>
        public string DependencyPropertyName => $"{Name}Property";
    }
}
