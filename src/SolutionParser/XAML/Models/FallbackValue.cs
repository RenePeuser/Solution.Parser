using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class FallbackValue
    {
        public FallbackValue(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
