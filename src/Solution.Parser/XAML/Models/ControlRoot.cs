using System.Collections.Generic;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(XName) + "}")]
    public class ControlRoot : Root
    {
        internal ControlRoot(DataContext dataContext, ElementBase parent, string fullQualifiedName, string xName,
            string typeName, string xKey, IEnumerable<Property> properties, IEnumerable<ElementBase> controls,
            IEnumerable<Style> styles, IEnumerable<DataTemplate> dataTemplates) : base(dataContext, parent,
            fullQualifiedName, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
