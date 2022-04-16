using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal interface ISpecificControlBuilderSelector
    {
        ISpecificControlBuilder GetBuilderFor(XElement xElement);
    }
}
