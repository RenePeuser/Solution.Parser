using System.Xml.Linq;

namespace SolutionParser.XAML
{
    public interface IXamlParser
    {
        XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo);
        XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo, string xamlContent);
        ElementBase Parse(XElement element);
    }
}
