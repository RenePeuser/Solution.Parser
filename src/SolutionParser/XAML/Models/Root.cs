using System.Collections.Generic;
using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(TypeName) + "}")]
    public abstract class Root : Control
    {
        protected Root(DataContext dataContext, ElementBase parent, string fullQualifiedName, string xName,
            string typeName, string xKey, IEnumerable<Property> properties, IEnumerable<ElementBase> controls,
            IEnumerable<Style> styles, IEnumerable<DataTemplate> dataTemplates) : base(1, dataContext, parent,
            xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
            FullQualifiedName = fullQualifiedName;
        }

        public string FullQualifiedName { get; }
    }
}
