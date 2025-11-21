using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal interface IControlBuilder
    {
        IEnumerable<ElementBase> BuildFrom(ImmutableList<XElement> elements, ElementBase? parent);
        ElementBase BuildFrom(XElement element, ElementBase? parent);
    }
}
