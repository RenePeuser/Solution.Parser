using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal interface IXamlPropertyParser
    {
        Property ParseFrom(XAttribute attribute);
    }
}
