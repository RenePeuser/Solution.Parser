using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal interface IRootSelector
    {
        IConcreteRootBuilder GetRootBuilderFor(XDocument document);
    }
}
