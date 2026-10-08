using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Argument.Check;
using Solution.Parser.Project;

namespace Solution.Parser.Sln
{
    [DebuggerDisplay("{SolutionFileInfo.Name}")]
    public class SolutionFile
    {
        internal SolutionFile(SolutionFileInfo solutionFileInfo,
            ImmutableList<ProjectFile> projectFiles,
            ImmutableList<ProjectFile> productiveProjectFiles,
            ImmutableList<ProjectFile> unitTestProjectFiles)
        {
            SolutionFileInfo = solutionFileInfo;
            Projects = projectFiles;
            ProductionProjects = productiveProjectFiles;
            UnitTestProjects = unitTestProjectFiles;
        }

        public SolutionFileInfo SolutionFileInfo { get; }

        public ImmutableList<ProjectFile> Projects { get; }

        public ImmutableList<ProjectFile> ProductionProjects { get; }

        public ImmutableList<ProjectFile> UnitTestProjects { get; }

        /// <summary>
        /// Finds the solution from <paramref name="startDirectory"/> upwards and parses it. Without a start
        /// directory the search starts where the running assembly is, which for a test project is inside
        /// the repository.
        /// </summary>
        /// <param name="solutionName">
        /// <c>MySolution.sln</c> or <c>MySolution.slnx</c> for exactly that file; <c>MySolution</c> for either
        /// of both, where <c>.slnx</c> wins when both exist side by side.
        /// </param>
        /// <param name="startDirectory">Where the search starts, the directory of the running assembly by default.</param>
        public static SolutionFile Find(string solutionName, DirectoryInfo? startDirectory = null)
        {
            Throw.IfNullOrWhiteSpace(solutionName);

            var name = SolutionFileFormat.All.Any(f => solutionName.EndsWith(f, StringComparison.OrdinalIgnoreCase))
                           ? new SolutionFileName(solutionName)
                           : SolutionFileName.WithAnySolutionFormat(solutionName);

            return name.FindSolutionFileReverseFrom(startDirectory ?? new DirectoryInfo(AppContext.BaseDirectory)).Parse();
        }

        /// <summary>The project with this name, the file name of its csproj without extension.</summary>
        /// <exception cref="ArgumentException">The solution has no such project; the message lists the ones it has.</exception>
        public ProjectFile Project(string projectName)
        {
            Throw.IfNullOrWhiteSpace(projectName);

            return Projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException($"The solution '{SolutionFileInfo.Name}' has no project '{projectName}'. "
                                                  + $"Its projects are: {string.Join(", ", Projects.Select(p => p.Name).Order(StringComparer.Ordinal))}.",
                                                  nameof(projectName));
        }
    }
}
