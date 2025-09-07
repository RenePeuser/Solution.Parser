namespace Solution.Parser.XAML
{
    public class XAMLFileInfo(string path) : SpecificFileInfoBase(path, ".xaml"), IXamlFileInfo;
}
