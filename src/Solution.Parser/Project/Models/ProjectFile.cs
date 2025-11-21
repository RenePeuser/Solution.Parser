using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;
using Argument.Check;
using Solution.Parser.CSharp;
using Solution.Parser.XAML;

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
                             ImmutableList<CSharpFileInfo> csharpFileInfos,
                             ImmutableList<XAMLFileInfo> xamlFileInfos,
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
            Throw.IfNull(csharpFileInfos);
            Throw.IfNull(xamlFileInfos);
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
            CSharpFileInfos = csharpFileInfos;
            XAMLFileInfos = xamlFileInfos;
            ContentItems = projectContentItems;
            TargetFrameworkVersion = targetFrameworkVersion;
            BuildDependencies = buildDependencies;
            BuildRoot = buildRoot;
            DocumentationFile = documentationFile;
            Packages = packages;
            PackageReferences = packageReferences;
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

        public ImmutableList<XAMLFileInfo> XAMLFileInfos { get; }

        public ImmutableList<CSharpFileInfo> CSharpFileInfos { get; }

        public ImmutableList<ProjectContentItem> ContentItems { get; }

        public ImmutableList<Package> Packages { get; }

        public ImmutableList<ProjectFile> BuildDependencies { get; }

        public ImmutableList<string> TargetFrameworkVersion { get; }

        public string BuildRoot { get; }

        public string DocumentationFile { get; }
    }
}
