using System.Diagnostics;

namespace Solution.Parser.Nuspec
{
    [DebuggerDisplay("{Value.FullName}")]
    public class NuspecFileInfo(string path) : SpecificFileInfoBase(path, ".nuspec");
}
