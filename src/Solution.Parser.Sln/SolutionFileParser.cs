using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using Argument.Check;
using Extensions.Pack;
using Microsoft.Build.Construction;
using Solution.Parser.Project;

namespace Solution.Parser.Sln
{
    internal static class SolutionFileParser
    {
        private const string PROJECT_FILE_EXTENSION = ".csproj";

        internal static SolutionFile Parse(FileInfo solutionFileInfo)
        {
            Throw.IfNull(solutionFileInfo);

            if (!SolutionFileFormat.All.Any(format => solutionFileInfo.Name.EndsWith(format, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"The given path '{solutionFileInfo.FullName}' has not one of the expected file extensions '{string.Join("', '", SolutionFileFormat.All)}'");
            }

            // var nuspecFiles = NuGetFolderParser.Parse(NugetFolderFinder.GetUserNuGetFolder()).ToImmutableList();
            var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(solutionFileInfo.FullName);

            // Every project is read and its directory walked on its own, so they are parsed in parallel,
            // in order. A failure surfaces as it would without the parallelism.
            ImmutableList<ProjectToProjectSolutionItem> tempProjects;

            try
            {
                tempProjects = solutionFile.ProjectsInOrder
                                           .Where(item => Path.GetExtension(item.RelativePath).EqualsTo(PROJECT_FILE_EXTENSION))
                                           .AsParallel()
                                           .AsOrdered()
                                           .Select(item => new ProjectToProjectSolutionItem(item, ProjectFile.Parse(item.AbsolutePath)))
                                           .ToImmutableList();
            }
            catch (AggregateException e) when (e.InnerExceptions.Count >= 1)
            {
                ExceptionDispatchInfo.Throw(e.InnerExceptions[0]);

                throw;
            }

            var projects = tempProjects.Select(item => PrepareWithBuildDependencies(item, tempProjects)).ToImmutableList();

            var unitTestProjects = projects.Where(item => item.ProjectTypes.Any(type => type.EqualsTo(ProjectType.Test))).ToImmutableList();
            var productiveProjects = projects.Except(unitTestProjects).ToImmutableList();

            return new SolutionFile(solutionFileInfo, projects, productiveProjects,
                                    unitTestProjects);
        }

        private static ProjectFile PrepareWithBuildDependencies(ProjectToProjectSolutionItem projectToProjectSolutionItem,
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
            internal ProjectToProjectSolutionItem(ProjectInSolution projectInSolution,
                                                  ProjectFile projectFile)
            {
                ProjectInSolution = projectInSolution;
                ProjectFile = projectFile;
            }

            internal ProjectInSolution ProjectInSolution { get; }

            internal ProjectFile ProjectFile { get; }
        }
    }
}
