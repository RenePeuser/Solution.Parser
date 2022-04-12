namespace SolutionParser.XAML
{
    public class XAMLFileInfo : SpecificFileInfoBase, IXamlFileInfo
    {
        public XAMLFileInfo(string path)
            : base(path, ".xaml")
        {
        }
    }
}
