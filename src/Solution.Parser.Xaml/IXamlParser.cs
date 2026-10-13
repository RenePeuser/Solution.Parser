using System.IO;
using System.Xml.Linq;

namespace Solution.Parser.Xaml
{
    public interface IXamlParser
    {
        XamlSyntaxTree Parse(FileInfo xamlFileInfo);

        XamlSyntaxTree Parse(FileInfo xamlFileInfo, string xamlContent);

        XamlSyntaxTree ParseContent(string xamlContent, string filePath);

        ElementBase Parse(XElement element);
    }
}
