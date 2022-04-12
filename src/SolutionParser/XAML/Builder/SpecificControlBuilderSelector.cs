using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal class SpecificControlBuilderSelector : ISpecificControlBuilderSelector
    {
        private readonly IEnumerable<ISpecificControlBuilder> _specificControlBuilders;

        internal SpecificControlBuilderSelector()
            : this(new ISpecificControlBuilder[]
            {
                new DataTemplateBuilder(), new ControlBuilder(), new ResourceDictionaryBuilder()
            })
        {
        }

        internal SpecificControlBuilderSelector(IEnumerable<ISpecificControlBuilder> specificControlBuilders)
        {
            _specificControlBuilders = specificControlBuilders.ToList();
        }

        public ISpecificControlBuilder GetBuilderFor(XElement xElement)
        {
            return _specificControlBuilders.FirstOrDefault(builder => builder.IsThisTheBuilderFor(xElement));
        }
    }
}
