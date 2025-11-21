using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("Name:{XName} Key:{XKey}")]
    public class Control : ElementBase
    {
        internal Control(int lineNumber, DataContext? dataContext, ElementBase? parent, string xName,
            string typeName, string xKey, ImmutableList<Property> properties, ImmutableList<ElementBase> controls,
            ImmutableList<Style> styles, ImmutableList<DataTemplate> dataTemplates) : base(lineNumber, dataContext,
            parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
