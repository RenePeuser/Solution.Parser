using System.Collections.Generic;
using System.Diagnostics;
using Solution.Parser.Project;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{SolutionFileInfo.Value.Name}")]
    public class SolutionFile
    {
        internal SolutionFile(SolutionFileInfo solutionFileInfo,
            IEnumerable<ProjectFile> projectFiles,
            IEnumerable<ProjectFile> productiveProjectFiles,
            IEnumerable<ProjectFile> unitTestProjectFiles)
        {
            SolutionFileInfo = solutionFileInfo;
            Projects = projectFiles;
            ProductiveProjects = productiveProjectFiles;
            UnitTestProjects = unitTestProjectFiles;
        }

        public SolutionFileInfo SolutionFileInfo { get; }

        public IEnumerable<ProjectFile> Projects { get; }

        public IEnumerable<ProjectFile> ProductiveProjects { get; }

        public IEnumerable<ProjectFile> UnitTestProjects { get; }
    }
}
