using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{TypeName}")]
    public class ResourceDictionary : Root
    {
        internal ResourceDictionary(DataContext? dataContext, ElementBase? parent, string fullQualifiedName,
            string xName, string typeName, string xKey, ImmutableList<Property> properties,
            ImmutableList<ElementBase> controls, ImmutableList<Style> styles, ImmutableList<DataTemplate> dataTemplates)
            : base(dataContext, parent, fullQualifiedName, xName, typeName, xKey, properties, controls, styles,
                dataTemplates)
        {
        }
    }
}
