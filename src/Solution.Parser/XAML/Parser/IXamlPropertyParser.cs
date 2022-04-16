using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal interface IXamlPropertyParser
    {
        Property ParseFrom(XAttribute attribute);
    }
}
