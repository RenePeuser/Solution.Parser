using Solution.Parser.Core;

namespace Solution.Parser.Xaml
{
    public class XamlFileInfo(string path) : SpecificFileInfo(path, ".xaml");
}
