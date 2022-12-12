using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Type}")]
    public class Window : UserControl
    {
        internal Window(DataContext? dataContext, ElementBase? parent, string fullQualifiedName, string xName,
            string typeName, string xKey, IImmutableList<Property> properties, IImmutableList<ElementBase> controls,
            IImmutableList<Style> styles, IImmutableList<DataTemplate> dataTemplates) : base(dataContext, parent,
            fullQualifiedName, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
