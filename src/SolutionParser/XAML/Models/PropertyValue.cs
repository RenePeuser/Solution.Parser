using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class PropertyValue : ImmutableSemanticType<object>
    {
        internal PropertyValue(object value) : base(value)
        {
            ValueText = Value?.ToString();
        }

        public string ValueText { get; }
    }
}
