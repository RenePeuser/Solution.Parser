using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class StringFormat : PropertyValue
    {
        internal StringFormat(object value) : base(value)
        {
        }
    }
}
