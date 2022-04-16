using System.Collections.Generic;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(XKey) + "}")]
    public class DataTemplate : ElementBase
    {
        internal DataTemplate(int lineNumber, DataContext dataContext, ElementBase parent, string xName,
            string typeName, string xKey, IEnumerable<Property> properties, IEnumerable<ElementBase> controls,
            IEnumerable<Style> styles, IEnumerable<DataTemplate> dataTemplates) : base(lineNumber, dataContext,
            parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
