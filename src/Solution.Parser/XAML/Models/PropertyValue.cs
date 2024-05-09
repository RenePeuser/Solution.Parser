using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Value}")]
    public class PropertyValue : ImmutableSemanticType<object?>
    {
        internal PropertyValue(object? value) : base(value)
        {
            ValueText = Value?.ToString() ?? string.Empty;
        }

        public string ValueText { get; }
    }
}
