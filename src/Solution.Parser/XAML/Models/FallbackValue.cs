using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Value}")]
    public class FallbackValue(object value)
    {
        public object Value { get; } = value;
    }
}
