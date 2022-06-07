using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Value}")]
    public class XamlUsing : PropertyValue
    {
        internal XamlUsing(string value, string alias, string @namespace, string assembly) : base(value)
        {
            Alias = alias;
            Namespace = @namespace;
            Assembly = assembly;
        }

        public string Alias { get; }

        public string Namespace { get; }

        public string Assembly { get; }
    }
}
