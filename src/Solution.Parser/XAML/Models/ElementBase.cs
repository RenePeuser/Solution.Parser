using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Extensions.Pack;

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
            ImmutableList<Property> properties,
            ImmutableList<ElementBase> controls,
            ImmutableList<Style> styles,
            ImmutableList<DataTemplate> dataTemplates)
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

        public ImmutableList<Property> Properties { get; }

        public ImmutableList<ElementBase> Controls { get; }

        public ImmutableList<Style> Styles { get; }

        public ImmutableList<DataTemplate> DataTemplates { get; }

        public Property? this[string name] => FindPropertyByName(name);

        private Property? FindPropertyByName(string name)
        {
            if (name.Contains('.'))
            {
                return Properties.OfType<AttachedProperty>().FirstOrDefault(a => a.FullQualifiedName.EqualsTo(name)) ?? null;
            }

            return Properties.FirstOrDefault(p => p.Name.EqualsTo(name));
        }
    }
}
