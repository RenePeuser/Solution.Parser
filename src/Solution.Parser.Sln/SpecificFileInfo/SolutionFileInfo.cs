using System;
using Solution.Parser.Core;

namespace Solution.Parser.Sln
{
    public class SolutionFileInfo(string path) : SpecificFileInfo(path, SolutionFileFormat.SLNX, SolutionFileFormat.SLN)
    {
        public bool IsSlnx { get; } = path.EndsWith(SolutionFileFormat.SLNX, StringComparison.OrdinalIgnoreCase);
    }
}
