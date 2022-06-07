using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Value}")]
    public class StringFormat : PropertyValue
    {
        internal StringFormat(object value) : base(value)
        {
        }
    }
}
