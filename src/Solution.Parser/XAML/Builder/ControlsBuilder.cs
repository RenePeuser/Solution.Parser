using System.Collections.Generic;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal class ControlsBuilder : IControlBuilder
    {
        private readonly ISpecificControlBuilderSelector _specificControlBuilderSelector;

        internal ControlsBuilder() : this(new SpecificControlBuilderSelector())
        {
        }

        internal ControlsBuilder(ISpecificControlBuilderSelector specificControlBuilderSelector)
        {
            _specificControlBuilderSelector = specificControlBuilderSelector;
        }

        public IEnumerable<ElementBase> BuildFrom(IEnumerable<XElement> elements, ElementBase parent)
        {
            foreach (var element in elements)
            {
                var result = BuildFrom(element, parent);
                if (result.IsNull())
                {
                    continue;
                }

                yield return result;
            }
        }

        public ElementBase BuildFrom(XElement element, ElementBase parent)
        {
            var builder = _specificControlBuilderSelector.GetBuilderFor(element);
            return builder?.BuildFrom(element, parent);
        }
    }
}
