using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>A <c>&lt;Setter /&gt;</c> inside a <see cref="Style"/> or <see cref="Trigger"/>.</summary>
    [DebuggerDisplay("{PropertyName,nq} = {Value}")]
    public record Setter : ElementBase
    {
        public override ElementKind Kind => ElementKind.Setter;

        /// <summary>The value of the <c>Property</c> attribute, for example <c>Background</c>.</summary>
        public string PropertyName => this["Property"]?.PropertyValue?.ValueText ?? string.Empty;

        /// <summary>The value of the <c>Value</c> attribute, null when the setter uses a <c>&lt;Setter.Value&gt;</c> element.</summary>
        public PropertyValue? Value => this["Value"]?.PropertyValue;

        /// <summary>The value of the <c>TargetName</c> attribute, empty when the setter has none.</summary>
        public string TargetName => this["TargetName"]?.PropertyValue?.ValueText ?? string.Empty;
    }
}
