using System.Diagnostics;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{Value.FullName}")]
    public class SolutionFileInfo : SpecificFileInfoBase
    {
        public SolutionFileInfo(string path)
            : base(path, ".sln")
        {
        }
    }
}
