using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class StringFormat : PropertyValue
    {
        internal StringFormat(object value) : base(value)
        {
        }
    }
}
