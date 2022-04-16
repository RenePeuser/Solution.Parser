using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Argument.Check;
using Solution.Parser.CSharp;
using Solution.Parser.XAML;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{" + nameof(AssemblyName) + "}")]
    public class ProjectFile
    {
        internal ProjectFile(
            Guid guid,
            XDocument document,
            ProjectFileInfo projectFileInfo,
            string assemblyName,
            IEnumerable<AssemblyReference> assemblyReferences,
            IEnumerable<ProjectReference> projectReferences,
            IEnumerable<ProjectType> projectTypes,
            IEnumerable<Import> imports,
            IEnumerable<CSharpFileInfo> csharpFileInfos,
            IEnumerable<XAMLFileInfo> xamlFileInfos,
            IEnumerable<ProjectContentItem> projectContentItems,
            IEnumerable<Package> packages,
            IEnumerable<PackageReference> packageReferences,
            IEnumerable<string> targetFrameworkVersion,
            IEnumerable<ProjectFile> buildDependencies,
            string buildRoot,
            string documentationFile)
        {
            Throw.IfNull(() => document);
            Throw.IfNull(() => projectFileInfo);
            Throw.IfNull(() => assemblyReferences);
            Throw.IfNull(() => projectReferences);
            Throw.IfNull(() => projectTypes);
            Throw.IfNull(() => imports);
            Throw.IfNull(() => csharpFileInfos);
            Throw.IfNull(() => xamlFileInfos);
            Throw.IfNull(() => projectContentItems);
            Throw.IfNull(() => packages);
            Throw.IfNull(() => packageReferences);

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

        public IEnumerable<PackageReference> PackageReferences { get; }

        public string AssemblyName { get; }

        public IEnumerable<ProjectType> ProjectTypes { get; }

        public ProjectFileInfo ProjectFileInfo { get; }

        public Guid Guid { get; }

        public XDocument Document { get; }

        public IEnumerable<AssemblyReference> AssemblyReferences { get; }

        public IEnumerable<ProjectReference> ProjectReferences { get; }

        public IEnumerable<Import> Imports { get; }

        public IEnumerable<XAMLFileInfo> XAMLFileInfos { get; }

        public IEnumerable<CSharpFileInfo> CSharpFileInfos { get; }

        public IEnumerable<ProjectContentItem> ContentItems { get; }

        public IEnumerable<Package> Packages { get; }

        public IEnumerable<ProjectFile> BuildDependencies { get; }

        public IEnumerable<string> TargetFrameworkVersion { get; }

        public string BuildRoot { get; }

        public string DocumentationFile { get; }
    }
}
