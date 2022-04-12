using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Source) + "}")]
    public class RelativeSource : PropertyValue
    {
        internal RelativeSource(string value) : base(value)
        {
            Source = Value.ToString();
        }

        public string Source { get; }
    }
}
