using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Type}")]
    public class Window : UserControl
    {
        internal Window(DataContext? dataContext, ElementBase? parent, string fullQualifiedName, string xName,
            string typeName, string xKey, ImmutableList<Property> properties, ImmutableList<ElementBase> controls,
            ImmutableList<Style> styles, ImmutableList<DataTemplate> dataTemplates) : base(dataContext, parent,
            fullQualifiedName, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
