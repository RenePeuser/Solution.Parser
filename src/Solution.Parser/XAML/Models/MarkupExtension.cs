using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Name}")]
    public class MarkupExtension : PropertyValue
    {
        internal MarkupExtension(string value, string name) : this(value, name, ImmutableList<Property>.Empty)
        {
        }

        internal MarkupExtension(string value, string name, ImmutableList<Property> properties) : base(value)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }

        public ImmutableList<Property> Properties { get; }

        public Property? this[string name] => Properties.FirstOrDefault(p => p.Name.EqualsTo(name));
    }
}
