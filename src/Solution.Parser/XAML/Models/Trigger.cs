using System.Collections.Immutable;

namespace Solution.Parser.XAML
{
    public class Trigger : ElementBase
    {
        internal Trigger(int lineNumber, DataContext? dataContext, ElementBase? parent, string xName,
            string typeName, string xKey, ImmutableList<Property> properties, ImmutableList<ElementBase> controls,
            ImmutableList<Style> styles, ImmutableList<DataTemplate> dataTemplates) : base(lineNumber, dataContext,
            parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
