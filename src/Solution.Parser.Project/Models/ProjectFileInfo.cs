using Solution.Parser.Core;

namespace Solution.Parser.Project
{
    public class ProjectFileInfo(string path) : SpecificFileInfo(path, ".csproj");
}
