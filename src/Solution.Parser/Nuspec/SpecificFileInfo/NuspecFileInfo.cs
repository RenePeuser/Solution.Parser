using System.Diagnostics;

namespace Solution.Parser.Nuspec
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
