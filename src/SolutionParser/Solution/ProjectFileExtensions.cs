using System.Collections.Generic;
using SolutionParser.Project;

namespace SolutionParser.Solution
{
    internal static class ProjectFileExtensions
    {
        internal static ProjectFile UpdateProjectDependencies(this ProjectFile projectFile,
            IEnumerable<ProjectFile> buildDependencies)
        {
            return new ProjectFile(
                projectFile.Guid,
                projectFile.Document,
                projectFile.ProjectFileInfo,
                projectFile.AssemblyName,
                projectFile.AssemblyReferences,
                projectFile.ProjectReferences,
                projectFile.ProjectTypes,
                projectFile.Imports,
                projectFile.CSharpFileInfos,
                projectFile.XAMLFileInfos,
                projectFile.ContentItems,
                projectFile.Packages,
                projectFile.PackageReferences,
                projectFile.TargetFrameworkVersion,
                buildDependencies,
                projectFile.BuildRoot,
                projectFile.DocumentationFile);
        }
    }
}
