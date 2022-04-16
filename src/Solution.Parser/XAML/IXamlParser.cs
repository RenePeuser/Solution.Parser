using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    public interface IXamlParser
    {
        XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo);
        XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo, string xamlContent);
        ElementBase Parse(XElement element);
    }
}
