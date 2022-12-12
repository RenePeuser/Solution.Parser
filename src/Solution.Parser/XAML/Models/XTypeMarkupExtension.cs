using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Type}")]
    public class XTypeMarkupExtension : PropertyValue
    {
        internal XTypeMarkupExtension(string value) : base(value)
        {
            Type = Value?.ToString() ?? string.Empty;
        }

        public string Type { get; }
    }
}
