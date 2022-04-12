using System.Diagnostics;

namespace SolutionParser.Nuspec
{
    [DebuggerDisplay("{Value.FullName}")]
    public class NuspecFileInfo : SpecificFileInfoBase
    {
        public NuspecFileInfo(string path)
            : base(path, ".nuspec")
        {
        }
    }
}
