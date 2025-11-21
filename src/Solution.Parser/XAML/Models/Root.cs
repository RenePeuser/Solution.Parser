using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{TypeName}")]
    public abstract class Root(DataContext? dataContext,
                               ElementBase? parent,
                               string fullQualifiedName,
                               string xName,
                               string typeName,
                               string xKey,
                               ImmutableList<Property> properties,
                               ImmutableList<ElementBase> controls,
                               ImmutableList<Style> styles,
                               ImmutableList<DataTemplate> dataTemplates)
        : Control(1, dataContext, parent,
                  xName, typeName, xKey,
                  properties, controls, styles,
                  dataTemplates)
    {
        public string FullQualifiedName { get; } = fullQualifiedName;
    }
}
