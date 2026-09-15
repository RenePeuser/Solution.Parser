namespace Solution.Parser.CSharp
{
    public record CSharpFileInfo : SpecificFileInfoBase
    {
        public CSharpFileInfo(string path)
            : base(path, ".cs")
        {
        }
    }
}
