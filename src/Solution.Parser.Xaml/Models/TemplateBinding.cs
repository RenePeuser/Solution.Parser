using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>A <c>{TemplateBinding Property}</c>.</summary>
    [DebuggerDisplay("{PropertyName}")]
    public record TemplateBinding : MarkupExtension
    {
        internal TemplateBinding(string rawValue) : base(rawValue)
        {
        }

        /// <summary>The templated parent property being bound, empty when the extension names none.</summary>
        public required string PropertyName { get; init; }
    }
}
