using System.Collections.Generic;
using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(XKey) + "}")]
    public class Style : ElementBase
    {
        internal Style(int lineNumber, DataContext dataContext, ElementBase parent, string xName, string typeName,
            string xKey, IEnumerable<Property> properties, IEnumerable<ElementBase> controls,
            IEnumerable<Style> styles, IEnumerable<DataTemplate> dataTemplates) : base(lineNumber, dataContext,
            parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
