using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{XName}")]
    public class ResourceDictionaryControl : Control
    {
        internal ResourceDictionaryControl(int lineNumber, DataContext? dataContext, ElementBase? parent,
            string xName, string typeName, string xKey, ImmutableList<Property> properties,
            ImmutableList<ElementBase> controls, ImmutableList<Style> styles, ImmutableList<DataTemplate> dataTemplates)
            : base(lineNumber, dataContext, parent, xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
        }
    }
}
