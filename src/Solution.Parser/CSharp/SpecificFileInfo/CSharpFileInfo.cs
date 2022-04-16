namespace Solution.Parser.CSharp
{
    public class CSharpFileInfo : SpecificFileInfoBase
    {
        public CSharpFileInfo(string path)
            : base(path, ".cs")
        {
        }
    }
}
