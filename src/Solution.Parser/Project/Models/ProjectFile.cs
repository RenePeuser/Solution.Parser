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
        internal ProjectFile(
            Guid guid,
            XDocument document,
            ProjectFileInfo projectFileInfo,
            string assemblyName,
            IImmutableList<AssemblyReference> assemblyReferences,
            IImmutableList<ProjectReference> projectReferences,
            IImmutableList<ProjectType> projectTypes,
            IImmutableList<Import> imports,
            IImmutableList<CSharpFileInfo> csharpFileInfos,
            IImmutableList<XAMLFileInfo> xamlFileInfos,
            IImmutableList<ProjectContentItem> projectContentItems,
            IImmutableList<Package> packages,
            IImmutableList<PackageReference> packageReferences,
            IImmutableList<string> targetFrameworkVersion,
            IImmutableList<ProjectFile> buildDependencies,
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

        public IImmutableList<PackageReference> PackageReferences { get; }

        public string AssemblyName { get; }

        public IImmutableList<ProjectType> ProjectTypes { get; }

        public ProjectFileInfo ProjectFileInfo { get; }

        public Guid Guid { get; }

        public XDocument Document { get; }

        public IImmutableList<AssemblyReference> AssemblyReferences { get; }

        public IImmutableList<ProjectReference> ProjectReferences { get; }

        public IImmutableList<Import> Imports { get; }

        public IImmutableList<XAMLFileInfo> XAMLFileInfos { get; }

        public IImmutableList<CSharpFileInfo> CSharpFileInfos { get; }

        public IImmutableList<ProjectContentItem> ContentItems { get; }

        public IImmutableList<Package> Packages { get; }

        public IImmutableList<ProjectFile> BuildDependencies { get; }

        public IImmutableList<string> TargetFrameworkVersion { get; }

        public string BuildRoot { get; }

        public string DocumentationFile { get; }
    }
}
