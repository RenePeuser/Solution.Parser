using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Argument.Check;
using Solution.Parser.Project;

namespace Solution.Parser.Sln
{
    [DebuggerDisplay("{FileInfo.Name}")]
    public class SolutionFile
    {
        internal SolutionFile(FileInfo fileInfo,
            ImmutableList<ProjectFile> projectFiles,
            ImmutableList<ProjectFile> productiveProjectFiles,
            ImmutableList<ProjectFile> unitTestProjectFiles)
        {
            FileInfo = fileInfo;
            Projects = projectFiles;
            ProductionProjects = productiveProjectFiles;
            UnitTestProjects = unitTestProjectFiles;
        }

        public FileInfo FileInfo { get; }

        /// <summary>The solution name, the file name of the sln or slnx without extension.</summary>
        public string Name => System.IO.Path.GetFileNameWithoutExtension(FileInfo.Name);

        /// <summary>The full path of the sln or slnx.</summary>
        public string Path => FileInfo.FullName;

        public ImmutableList<ProjectFile> Projects { get; }

        public ImmutableList<ProjectFile> ProductionProjects { get; }

        public ImmutableList<ProjectFile> UnitTestProjects { get; }

        /// <summary>Parses the sln or slnx at <paramref name="path"/>.</summary>
        public static SolutionFile Parse(string path)
        {
            Throw.IfNullOrWhiteSpace(path);

            return Parse(new FileInfo(path));
        }

        /// <summary>Parses the given sln or slnx.</summary>
        public static SolutionFile Parse(FileInfo fileInfo)
        {
            return SolutionFileParser.Parse(fileInfo);
        }

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

            return Parse(name.FindSolutionFileReverseFrom(startDirectory ?? new DirectoryInfo(AppContext.BaseDirectory)));
        }

        /// <summary>The project with this name, the file name of its csproj without extension.</summary>
        /// <exception cref="ArgumentException">The solution has no such project; the message lists the ones it has.</exception>
        public ProjectFile Project(string projectName)
        {
            Throw.IfNullOrWhiteSpace(projectName);

            return Projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException($"The solution '{FileInfo.Name}' has no project '{projectName}'. "
                                                  + $"Its projects are: {string.Join(", ", Projects.Select(p => p.Name).Order(StringComparer.Ordinal))}.",
                                                  nameof(projectName));
        }
    }
}
