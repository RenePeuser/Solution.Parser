using System.Collections.Generic;

namespace Solution.Parser.XAML
{
    public class Trigger : ElementBase
    {
        internal Trigger(int lineNumber, DataContext dataContext, ElementBase parent, string xName,
            string typeName, string xKey, IEnumerable<Property> properties, IEnumerable<ElementBase> controls,
            IEnumerable<Style> styles, IEnumerable<DataTemplate> dataTemplates) : base(lineNumber, dataContext,
            parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
