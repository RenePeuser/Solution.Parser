using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Source}")]
    public class RelativeSource : PropertyValue
    {
        internal RelativeSource(string value) : base(value)
        {
            Source = Value.ToString();
        }

        public string Source { get; }
    }
}
