using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class UnknownPropertyValue : PropertyValue
    {
        internal UnknownPropertyValue(string value) : base(value)
        {
        }
    }
}
