using Solution.Parser.Core;

namespace Solution.Parser.Project
{
    public class AssemblyFileInfo : SpecificFileInfo
    {
        internal AssemblyFileInfo(string path)
            : base(path, ".dll")
        {
        }
    }
}
