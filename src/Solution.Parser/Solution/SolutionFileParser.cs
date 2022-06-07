using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Argument.Check;
using Microsoft.Build.Construction;
using Solution.Parser.Project;

namespace Solution.Parser.Solution
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
                .Where(item => Path.GetExtension(item.RelativePath) == PROJECT_FILE_EXTENSION)
                .Select(item => new { project = item, projectFileInfo = new ProjectFileInfo(item.AbsolutePath) })
                .Select(item => new ProjectToProjectSolutionItem(item.project, ProjectFileParser.Parse(item.projectFileInfo))).ToImmutableList();

            var projects = tempProjects.Select(item => PrepareWithBuildDependencies(item, tempProjects)).ToImmutableList();

            var unitTestProjects = projects.Where(item => item.ProjectTypes.Any(type => type == ProjectType.Test)).ToImmutableList();
            var productiveProjects = projects.Except(unitTestProjects).ToImmutableList();

            return new SolutionFile(solutionFileInfo, projects, productiveProjects, unitTestProjects);
        }

        private static ProjectFile PrepareWithBuildDependencies(
            ProjectToProjectSolutionItem projectToProjectSolutionItem,
            IImmutableList<ProjectToProjectSolutionItem> allProjectFiles)
        {
            var projectToCheck = projectToProjectSolutionItem.ProjectInSolution;
            var projctFile = projectToProjectSolutionItem.ProjectFile;

            var projectDependencies = projectToCheck.Dependencies.Select(d => new Guid(d)).ToImmutableList();
            var allProjects = allProjectFiles.Select(a => a.ProjectFile).ToImmutableList();

            var buildDependencies = allProjects.Where(p => projectDependencies.Contains(p.Guid)).ToImmutableList();

            var newProject = projctFile.UpdateProjectDependencies(buildDependencies);
            return newProject;
        }

        private class ProjectToProjectSolutionItem
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
