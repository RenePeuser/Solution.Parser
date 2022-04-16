using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal interface IRootBuilder
    {
        Root BuildFrom(XDocument document, IXamlFileInfo xamlFileInfo);
    }
}
