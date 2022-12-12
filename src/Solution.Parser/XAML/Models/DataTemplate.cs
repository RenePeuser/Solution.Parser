using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{XKey}")]
    public class DataTemplate : ElementBase
    {
        internal DataTemplate(int lineNumber, DataContext? dataContext, ElementBase? parent, string xName,
            string typeName, string xKey, IImmutableList<Property> properties, IImmutableList<ElementBase> controls,
            IImmutableList<Style> styles, IImmutableList<DataTemplate> dataTemplates) : base(lineNumber, dataContext,
            parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
