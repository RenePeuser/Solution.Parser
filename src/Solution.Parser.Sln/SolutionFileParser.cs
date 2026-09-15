using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
using Microsoft.Build.Construction;
using Solution.Parser.Project;

namespace Solution.Parser.Sln
{
    public static class SolutionFileParser
    {
        private const string PROJECT_FILE_EXTENSION = ".csproj";

        public static SolutionFile Parse(this SolutionFileInfo solutionFileInfo)
        {
            Throw.IfNull(solutionFileInfo);

            // var nuspecFiles = NuGetFolderParser.Parse(NugetFolderFinder.GetUserNuGetFolder()).ToImmutableList();
            var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(solutionFileInfo.Value.FullName);

            var tempProjects = solutionFile.ProjectsInOrder
                .Where(item => Path.GetExtension(item.RelativePath).EqualsTo(PROJECT_FILE_EXTENSION))
                .Select(item => new { project = item, projectFileInfo = new ProjectFileInfo(item.AbsolutePath) })
                .Select(item => new ProjectToProjectSolutionItem(item.project, ProjectFileParser.Parse(item.projectFileInfo))).ToImmutableList();

            var projects = tempProjects.Select(item => PrepareWithBuildDependencies(item, tempProjects)).ToImmutableList();

            var unitTestProjects = projects.Where(item => item.ProjectTypes.Any(type => type.EqualsTo(ProjectType.Test))).ToImmutableList();
            var productiveProjects = projects.Except(unitTestProjects).ToImmutableList();

            return new SolutionFile(solutionFileInfo, projects, productiveProjects, unitTestProjects);
        }

        private static ProjectFile PrepareWithBuildDependencies(
            ProjectToProjectSolutionItem projectToProjectSolutionItem,
            ImmutableList<ProjectToProjectSolutionItem> allProjectFiles)
        {
            var projectToCheck = projectToProjectSolutionItem.ProjectInSolution;
            var projctFile = projectToProjectSolutionItem.ProjectFile;

            // The guids have to be compared on solution level. Sdk style projects (and therefore every project inside a slnx)
            // do not carry a ProjectGuid anymore, so the guid of the parsed project file would always be empty here.
            var projectDependencies = projectToCheck.Dependencies.Select(dependency => new Guid(dependency)).ToImmutableHashSet();

            var buildDependencies = allProjectFiles.Where(item => projectDependencies.Contains(new Guid(item.ProjectInSolution.ProjectGuid)))
                                                   .Select(item => item.ProjectFile)
                                                   .ToImmutableList();

            var newProject = projctFile.WithBuildDependencies(buildDependencies);
            return newProject;
        }

        private sealed class ProjectToProjectSolutionItem
        {
            internal ProjectToProjectSolutionItem(ProjectInSolution projectInSolution, ProjectFile projectFile)
            {
                ProjectInSolution = projectInSolution;
                ProjectFile = projectFile;
            }

            internal ProjectInSolution ProjectInSolution { get; }

            internal ProjectFile ProjectFile { get; }
        }
    }
}
