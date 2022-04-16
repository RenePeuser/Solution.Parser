using System.Diagnostics;
using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{FileInfo.Value.Name}")]
    public class XamlSyntaxTree
    {
        internal XamlSyntaxTree(IXamlFileInfo xamlFileInfo, XDocument document, Root root)
        {
            FileInfo = xamlFileInfo;
            Document = document;
            Root = root;
        }

        public IXamlFileInfo FileInfo { get; }

        public Root Root { get; }

        public XDocument Document { get; }
    }
}
