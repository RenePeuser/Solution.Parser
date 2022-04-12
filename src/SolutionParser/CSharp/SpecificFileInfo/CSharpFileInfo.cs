namespace SolutionParser.CSharp
{
    public class CSharpFileInfo : SpecificFileInfoBase
    {
        public CSharpFileInfo(string path)
            : base(path, ".cs")
        {
        }
    }
}
