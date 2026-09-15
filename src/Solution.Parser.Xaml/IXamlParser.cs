using System.Xml.Linq;

namespace Solution.Parser.Xaml
{
    public interface IXamlParser
    {
        XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo);

        XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo, string xamlContent);

        XamlSyntaxTree ParseContent(string xamlContent, string filePath);

        ElementBase Parse(XElement element);
    }
}
