using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(TypeName) + "}")]
    public abstract class ElementBase
    {
        internal ElementBase(
            int lineNumber,
            DataContext dataContext,
            ElementBase parent,
            string xName,
            string typeName,
            string xKey,
            IEnumerable<Property> properties,
            IEnumerable<ElementBase> controls,
            IEnumerable<Style> styles,
            IEnumerable<DataTemplate> dataTemplates)
        {
            LineNumber = lineNumber;
            DataContext = dataContext;
            Parent = parent;
            XName = xName;
            TypeName = typeName;
            XKey = xKey;
            Properties = properties;
            Controls = controls;
            Styles = styles;
            DataTemplates = dataTemplates;
        }

        public int LineNumber { get; }

        public DataContext DataContext { get; }

        public ElementBase Parent { get; }

        public string TypeName { get; }

        public string XName { get; }

        public string XKey { get; }

        public IEnumerable<Property> Properties { get; }

        public IEnumerable<ElementBase> Controls { get; }

        public IEnumerable<Style> Styles { get; }

        public IEnumerable<DataTemplate> DataTemplates { get; }

        public Property this[string name] => FindPropertyByName(name);

        private Property FindPropertyByName(string name)
        {
            if (name.Contains('.'))
            {
                return Properties.OfType<AttachedProperty>().FirstOrDefault(a => a.FullQualifiedName == name);
            }

            return Properties.FirstOrDefault(p => p.Name == name);
        }
    }
}
