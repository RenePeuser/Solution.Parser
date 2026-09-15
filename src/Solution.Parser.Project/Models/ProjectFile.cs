using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{AssemblyName}")]
    public class ProjectFile
    {
        internal ProjectFile(Guid guid,
                             XDocument document,
                             ProjectFileInfo projectFileInfo,
                             string assemblyName,
                             ImmutableList<AssemblyReference> assemblyReferences,
                             ImmutableList<ProjectReference> projectReferences,
                             ImmutableList<ProjectType> projectTypes,
                             ImmutableList<Import> imports,
                             ImmutableList<FileInfo> sourceFiles,
                             ImmutableList<ProjectContentItem> projectContentItems,
                             ImmutableList<Package> packages,
                             ImmutableList<PackageReference> packageReferences,
                             ImmutableList<string> targetFrameworkVersion,
                             ImmutableList<ProjectFile> buildDependencies,
                             string buildRoot,
                             string documentationFile)
        {
            Throw.IfNull(document);
            Throw.IfNull(projectFileInfo);
            Throw.IfNull(assemblyReferences);
            Throw.IfNull(projectReferences);
            Throw.IfNull(projectTypes);
            Throw.IfNull(imports);
            Throw.IfNull(sourceFiles);
            Throw.IfNull(projectContentItems);
            Throw.IfNull(packages);
            Throw.IfNull(packageReferences);

            Guid = guid;
            Document = document;
            ProjectFileInfo = projectFileInfo;
            AssemblyName = assemblyName;
            AssemblyReferences = assemblyReferences;
            ProjectReferences = projectReferences;
            ProjectTypes = projectTypes;
            Imports = imports;
            SourceFiles = sourceFiles;
            ContentItems = projectContentItems;
            TargetFrameworkVersion = targetFrameworkVersion;
            BuildDependencies = buildDependencies;
            BuildRoot = buildRoot;
            DocumentationFile = documentationFile;
            Packages = packages;
            PackageReferences = packageReferences;
        }

        /// <summary>
        /// The same project with its build dependencies filled in. Resolving those needs the whole
        /// solution, so the solution package asks for this copy rather than reaching for the
        /// constructor, which it can no longer see across the assembly boundary.
        /// </summary>
        public ProjectFile WithBuildDependencies(ImmutableList<ProjectFile> buildDependencies)
        {
            Throw.IfNull(buildDependencies);

            return new ProjectFile(Guid,
                                   Document,
                                   ProjectFileInfo,
                                   AssemblyName,
                                   AssemblyReferences,
                                   ProjectReferences,
                                   ProjectTypes,
                                   Imports,
                                   SourceFiles,
                                   ContentItems,
                                   Packages,
                                   PackageReferences,
                                   TargetFrameworkVersion,
                                   buildDependencies,
                                   BuildRoot,
                                   DocumentationFile);
        }

        public ImmutableList<PackageReference> PackageReferences { get; }

        public string AssemblyName { get; }

        public ImmutableList<ProjectType> ProjectTypes { get; }

        public ProjectFileInfo ProjectFileInfo { get; }

        public Guid Guid { get; }

        public XDocument Document { get; }

        public ImmutableList<AssemblyReference> AssemblyReferences { get; }

        public ImmutableList<ProjectReference> ProjectReferences { get; }

        public ImmutableList<Import> Imports { get; }

        /// <summary>
        /// Every file that belongs to the project, without deciding what kind of file it is.
        /// </summary>
        /// <remarks>
        /// The typed views live in the language packages, as <c>SourceFiles.CSharpFiles()</c> and
        /// <c>SourceFiles.XamlFiles()</c>. That is what keeps this package language agnostic: while
        /// <c>ProjectFile</c> itself carried <c>CSharpFileInfos</c> and <c>XamlFileInfos</c>, anyone
        /// reading a solution had to pull both languages whether they used them or not.
        /// </remarks>
        public ImmutableList<FileInfo> SourceFiles { get; }

        public ImmutableList<ProjectContentItem> ContentItems { get; }

        public ImmutableList<Package> Packages { get; }

        public ImmutableList<ProjectFile> BuildDependencies { get; }

        public ImmutableList<string> TargetFrameworkVersion { get; }

        public string BuildRoot { get; }

        public string DocumentationFile { get; }
    }
}
