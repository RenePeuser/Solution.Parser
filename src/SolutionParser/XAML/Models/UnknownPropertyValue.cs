using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class UnknownPropertyValue : PropertyValue
    {
        internal UnknownPropertyValue(string value) : base(value)
        {
        }
    }
}
