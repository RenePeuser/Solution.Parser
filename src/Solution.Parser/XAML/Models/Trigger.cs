using System.Collections.Immutable;

namespace Solution.Parser.XAML
{
    public class Trigger : ElementBase
    {
        internal Trigger(int lineNumber, DataContext dataContext, ElementBase parent, string xName,
            string typeName, string xKey, IImmutableList<Property> properties, IImmutableList<ElementBase> controls,
            IImmutableList<Style> styles, IImmutableList<DataTemplate> dataTemplates) : base(lineNumber, dataContext,
            parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
