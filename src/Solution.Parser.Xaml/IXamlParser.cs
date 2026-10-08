using System.Xml.Linq;

namespace Solution.Parser.Xaml
{
    public interface IXamlParser
    {
        XamlSyntaxTree Parse(XamlFileInfo xamlFileInfo);

        XamlSyntaxTree Parse(XamlFileInfo xamlFileInfo, string xamlContent);

        XamlSyntaxTree ParseContent(string xamlContent, string filePath);

        ElementBase Parse(XElement element);
    }
}
