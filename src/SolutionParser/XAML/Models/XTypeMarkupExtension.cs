using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Type) + "}")]
    public class XTypeMarkupExtension : PropertyValue
    {
        internal XTypeMarkupExtension(string value) : base(value)
        {
            Type = Value.ToString();
        }

        public string Type { get; }
    }
}
