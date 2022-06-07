using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Value}")]
    public class FallbackValue
    {
        public FallbackValue(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
