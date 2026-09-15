using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>A <c>&lt;Style /&gt;</c>, whether declared in a resource dictionary or inline.</summary>
    [DebuggerDisplay("Key:{XKey} TargetType:{TargetTypeName}")]
    public record Style : ElementBase
    {
        public override ElementKind Kind => ElementKind.Style;

        /// <summary>The value of <c>TargetType</c> with any prefix stripped, empty when the style declares none.</summary>
        public string TargetTypeName => (this["TargetType"]?.PropertyValue).ToTypeName();

        /// <summary>The resource key this style derives from, empty when it derives from nothing.</summary>
        public string BasedOn => (this["BasedOn"]?.PropertyValue as StaticResource)?.ResourceKey ?? string.Empty;

        /// <summary>The setters declared directly in this style.</summary>
        public ImmutableList<Setter> Setters => Children.OfType<Setter>().ToImmutableList();

        /// <summary>The triggers declared in this style, which XAML writes as a <c>&lt;Style.Triggers&gt;</c> property element.</summary>
        public ImmutableList<Trigger> Triggers =>
            Children.OfType<Trigger>()
                    .Concat(PropertyElements.Where(p => p.Name.EqualsTo("Triggers"))
                                            .SelectMany(p => p.Children)
                                            .OfType<Trigger>())
                    .ToImmutableList();
    }
}
