using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Type}")]
    public class XStaticMarkupExtension : PropertyValue
    {
        internal XStaticMarkupExtension(string value) : base(value)
        {
            Type = Value?.ToString() ?? string.Empty;
        }

        public string Type { get; }
    }
}
