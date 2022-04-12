using System.Diagnostics;

namespace SolutionParser.Solution
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
