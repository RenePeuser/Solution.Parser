using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Argument.Check;
using Microsoft.Build.Construction;
using Solution.Parser.Project;
using SolutionFile = Solution.Parser.Solution.SolutionFile;

namespace Solution.Parser.Solution
{
    public static class SolutionFileParser
    {
        private const string PROJECT_FILE_EXTENSION = ".csproj";

        public static SolutionFile Parse(this SolutionFileInfo solutionFileInfo)
        {
            Throw.IfNull(() => solutionFileInfo);

            // var nuspecFiles = NuGetFolderParser.Parse(NugetFolderFinder.GetUserNuGetFolder()).ToList();
            var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(solutionFileInfo.Value.FullName);

            var tempProjects = solutionFile.ProjectsInOrder
                .Where(item => Path.GetExtension(item.RelativePath) == PROJECT_FILE_EXTENSION)
                .Select(item => new { project = item, projectFileInfo = new ProjectFileInfo(item.AbsolutePath) })
                .Select(item => new ProjectToProjectSolutionItem(item.project, ProjectFileParser.Parse(item.projectFileInfo))).ToList();

            var projects = tempProjects.Select(item => PrepareWithBuildDependencies(item, tempProjects)).ToList();

            var unitTestProjects = projects.Where(item => item.ProjectTypes.Any(type => type == ProjectType.Test)).ToList();
            var productiveProjects = projects.Except(unitTestProjects).ToList();

            return new SolutionFile(solutionFileInfo, projects, productiveProjects, unitTestProjects);
        }

        private static ProjectFile PrepareWithBuildDependencies(
            ProjectToProjectSolutionItem projectToProjectSolutionItem,
            IEnumerable<ProjectToProjectSolutionItem> allProjectFiles)
        {
            var projectToCheck = projectToProjectSolutionItem.ProjectInSolution;
            var projctFile = projectToProjectSolutionItem.ProjectFile;

            var projectDependencies = projectToCheck.Dependencies.Select(d => new Guid(d)).ToList();
            var allProjects = allProjectFiles.Select(a => a.ProjectFile).ToList();

            var buildDependencies = allProjects.Where(p => projectDependencies.Contains(p.Guid)).ToList();

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
