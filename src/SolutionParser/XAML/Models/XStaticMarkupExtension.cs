using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Type) + "}")]
    public class XStaticMarkupExtension : PropertyValue
    {
        internal XStaticMarkupExtension(string value) : base(value)
        {
            Type = Value.ToString();
        }

        public string Type { get; }
    }
}
