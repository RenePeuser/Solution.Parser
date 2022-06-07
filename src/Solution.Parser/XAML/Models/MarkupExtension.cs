using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class MarkupExtension : PropertyValue
    {
        internal MarkupExtension(string value, string name) : this(value, name, ImmutableList<Property>.Empty)
        {
        }

        internal MarkupExtension(string value, string name, IImmutableList<Property> properties) : base(value)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }

        public IImmutableList<Property> Properties { get; }

        public Property this[string name] => Properties.FirstOrDefault(p => p.Name == name);
    }
}
