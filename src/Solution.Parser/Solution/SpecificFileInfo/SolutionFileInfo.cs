using System.Diagnostics;
using Extensions.Pack;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{Value.FullName}")]
    public class SolutionFileInfo(string path) : SpecificFileInfoBase(path, SolutionFileFormat.SLNX, SolutionFileFormat.SLN)
    {
        public bool IsSlnx { get; } = path.EndWith(SolutionFileFormat.SLNX);
    }
}
