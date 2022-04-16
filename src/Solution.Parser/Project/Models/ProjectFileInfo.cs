using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Value.FullName}")]
    public class ProjectFileInfo : SpecificFileInfoBase
    {
        public ProjectFileInfo(string path)
            : base(path, ".csproj")
        {
        }
    }
}
