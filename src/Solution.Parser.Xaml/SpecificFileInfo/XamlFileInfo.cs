namespace Solution.Parser.Xaml
{
    public class XamlFileInfo(string path) : SpecificFileInfoBase(path, ".xaml"), IXamlFileInfo;
}
