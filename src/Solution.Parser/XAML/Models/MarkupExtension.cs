using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class MarkupExtension : PropertyValue
    {
        internal MarkupExtension(string value, string name) : this(value, name, Enumerable.Empty<Property>())
        {
        }

        internal MarkupExtension(string value, string name, IEnumerable<Property> properties) : base(value)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }

        public IEnumerable<Property> Properties { get; }

        public Property this[string name] => Properties.FirstOrDefault(p => p.Name == name);
    }
}
