using System.Diagnostics;

namespace Solution.Parser.XAML
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
