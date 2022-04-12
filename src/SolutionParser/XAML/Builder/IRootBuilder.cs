using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal interface IRootBuilder
    {
        Root BuildFrom(XDocument document, IXamlFileInfo xamlFileInfo);
    }
}
