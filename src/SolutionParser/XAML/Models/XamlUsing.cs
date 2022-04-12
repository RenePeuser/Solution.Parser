using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
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
