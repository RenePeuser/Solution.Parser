using System.Collections.Immutable;
using System.Diagnostics;
using Solution.Parser.Project;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{SolutionFileInfo.Value.Name}")]
    public class SolutionFile
    {
        internal SolutionFile(SolutionFileInfo solutionFileInfo,
            IImmutableList<ProjectFile> projectFiles,
            IImmutableList<ProjectFile> productiveProjectFiles,
            IImmutableList<ProjectFile> unitTestProjectFiles)
        {
            SolutionFileInfo = solutionFileInfo;
            Projects = projectFiles;
            ProductiveProjects = productiveProjectFiles;
            UnitTestProjects = unitTestProjectFiles;
        }

        public SolutionFileInfo SolutionFileInfo { get; }

        public IImmutableList<ProjectFile> Projects { get; }

        public IImmutableList<ProjectFile> ProductiveProjects { get; }

        public IImmutableList<ProjectFile> UnitTestProjects { get; }
    }
}
