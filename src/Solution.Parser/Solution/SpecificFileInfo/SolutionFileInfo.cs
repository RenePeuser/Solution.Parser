using System.Diagnostics;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{Value.FullName}")]
    public class SolutionFileInfo(string path) : SpecificFileInfoBase(path, ".sln");
}
