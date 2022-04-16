using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal interface IRootSelector
    {
        IConcreteRootBuilder GetRootBuilderFor(XDocument document);
    }
}
