using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{" + nameof(TypeName) + "}")]
    public abstract class Root : Control
    {
        protected Root(DataContext dataContext, ElementBase parent, string fullQualifiedName, string xName,
            string typeName, string xKey, IImmutableList<Property> properties, IImmutableList<ElementBase> controls,
            IImmutableList<Style> styles, IImmutableList<DataTemplate> dataTemplates) : base(1, dataContext, parent,
            xName, typeName, xKey, properties, controls, styles, dataTemplates)
        {
            FullQualifiedName = fullQualifiedName;
        }

        public string FullQualifiedName { get; }
    }
}
