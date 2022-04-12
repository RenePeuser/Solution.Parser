using System;
using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal interface ISpecificControlBuilder
    {
        Predicate<XElement> IsThisTheBuilderFor { get; }

        ElementBase BuildFrom(XElement element, ElementBase parent);
    }
}
