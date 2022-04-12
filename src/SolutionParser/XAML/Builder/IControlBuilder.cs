using System.Collections.Generic;
using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal interface IControlBuilder
    {
        IEnumerable<ElementBase> BuildFrom(IEnumerable<XElement> elements, ElementBase parent);
        ElementBase BuildFrom(XElement element, ElementBase parent);
    }
}
