using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Value.FullName}")]
    public class ProjectFileInfo(string path) : SpecificFileInfoBase(path, ".csproj");
}
