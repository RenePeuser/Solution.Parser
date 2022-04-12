using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal interface ISpecificControlBuilderSelector
    {
        ISpecificControlBuilder GetBuilderFor(XElement xElement);
    }
}
