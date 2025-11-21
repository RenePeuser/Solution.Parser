using System.Collections.Immutable;
using System.Diagnostics;
using Solution.Parser.Project;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{SolutionFileInfo.Value.Name}")]
    public class SolutionFile
    {
        internal SolutionFile(SolutionFileInfo solutionFileInfo,
            ImmutableList<ProjectFile> projectFiles,
            ImmutableList<ProjectFile> productiveProjectFiles,
            ImmutableList<ProjectFile> unitTestProjectFiles)
        {
            SolutionFileInfo = solutionFileInfo;
            Projects = projectFiles;
            ProductiveProjects = productiveProjectFiles;
            UnitTestProjects = unitTestProjectFiles;
        }

        public SolutionFileInfo SolutionFileInfo { get; }

        public ImmutableList<ProjectFile> Projects { get; }

        public ImmutableList<ProjectFile> ProductiveProjects { get; }

        public ImmutableList<ProjectFile> UnitTestProjects { get; }
    }
}
