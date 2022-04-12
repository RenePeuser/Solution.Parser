using System.Collections.Generic;
using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(TypeName) + "}")]
    public class ResourceDictionary : Root
    {
        internal ResourceDictionary(DataContext dataContext, ElementBase parent, string fullQualifiedName,
            string xName, string typeName, string xKey, IEnumerable<Property> properties,
            IEnumerable<ElementBase> controls, IEnumerable<Style> styles, IEnumerable<DataTemplate> dataTemplates)
            : base(dataContext, parent, fullQualifiedName, xName, typeName, xKey, properties, controls, styles,
                dataTemplates)
        {
        }
    }
}
