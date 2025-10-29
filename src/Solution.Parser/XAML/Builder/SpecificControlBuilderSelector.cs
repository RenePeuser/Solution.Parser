using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.XAML
{
    internal sealed class SpecificControlBuilderSelector : ISpecificControlBuilderSelector
    {
        private readonly IImmutableList<ISpecificControlBuilder> _specificControlBuilders = ImmutableList<ISpecificControlBuilder>.Empty;

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
            var builder = _specificControlBuilders.FirstOrDefault(builder => builder.IsThisTheBuilderFor(xElement));
            return Throw.IfNull(builder);
        }
    }
}
