using System.Diagnostics;

namespace SolutionParser.Project
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
