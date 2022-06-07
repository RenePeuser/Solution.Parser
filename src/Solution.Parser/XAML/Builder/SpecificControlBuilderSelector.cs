using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal class SpecificControlBuilderSelector : ISpecificControlBuilderSelector
    {
        private readonly IImmutableList<ISpecificControlBuilder> _specificControlBuilders;

        internal SpecificControlBuilderSelector()
            : this(new ISpecificControlBuilder[]
            {
                new DataTemplateBuilder(), new ControlBuilder(), new ResourceDictionaryBuilder()
            })
        {
        }

        internal SpecificControlBuilderSelector(IEnumerable<ISpecificControlBuilder> specificControlBuilders)
        {
            _specificControlBuilders = specificControlBuilders.ToImmutableList();
        }

        public ISpecificControlBuilder GetBuilderFor(XElement xElement)
        {
            return _specificControlBuilders.FirstOrDefault(builder => builder.IsThisTheBuilderFor(xElement));
        }
    }
}
