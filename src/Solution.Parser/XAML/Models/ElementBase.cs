using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{TypeName}")]
    public abstract class ElementBase
    {
        internal ElementBase(
            int lineNumber,
            DataContext? dataContext,
            ElementBase? parent,
            string xName,
            string typeName,
            string xKey,
            IImmutableList<Property> properties,
            IImmutableList<ElementBase> controls,
            IImmutableList<Style> styles,
            IImmutableList<DataTemplate> dataTemplates)
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

        public DataContext? DataContext { get; }

        public ElementBase? Parent { get; }

        public string TypeName { get; }

        public string XName { get; }

        public string XKey { get; }

        public IImmutableList<Property> Properties { get; }

        public IImmutableList<ElementBase> Controls { get; }

        public IImmutableList<Style> Styles { get; }

        public IImmutableList<DataTemplate> DataTemplates { get; }

        public Property? this[string name] => FindPropertyByName(name);

        private Property? FindPropertyByName(string name)
        {
            if (name.Contains('.'))
            {
                return Properties.OfType<AttachedProperty>().FirstOrDefault(a => a.FullQualifiedName == name) ?? null;
            }

            return Properties.FirstOrDefault(p => p.Name == name);
        }
    }
}
