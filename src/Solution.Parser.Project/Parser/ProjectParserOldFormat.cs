using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class ProjectParserOldFormat
    {
        internal static ProjectFile Parse(ProjectFileInfo projectFileInfo, XDocument document)
        {
            var guid = new Guid(document.ElementBy(ParserHelper.ProjectGuid)?.ValueOrDefault() ?? string.Empty);
            var projectReferences = GetReferences(document, ParserHelper.ProjectReference, ProjectReferenceParser.Parse);
            var assemblyReferences = GetReferences(document, ParserHelper.Reference, AssemblyReferenceParser.Parse);
            var projectTypes = AnalyzeProjectTypes(document);
            var assemblyName = document.ElementBy(ParserHelper.AssemblyName)?.ValueOrDefault() ?? string.Empty;
            var imports = GetAllImports(document);
            var sourceFiles = GetSourceFiles(document, projectFileInfo);
            var contentItems = GetContentItems(document, projectFileInfo);
            var packages = GetPackagesFrom(projectFileInfo, document);
            var packageReferences = GetPackageReferencesFrom(document);
            var targetFrameworkVersion = document.ElementBy(ParserHelper.TargetFrameworkVersion)?.ValueOrDefault().ToIList().FilterNullObjects().ToImmutableList() ?? ImmutableList<string>.Empty;
            var buildRoot = document.ElementBy(ParserHelper.BuildRoot)?.ValueOrDefault() ?? string.Empty;
            var documentationFile = document.ElementBy(ParserHelper.DocumentationFile)?.ValueOrDefault() ?? string.Empty;

            return new ProjectFile(guid,
                                   document,
                                   projectFileInfo,
                                   assemblyName,
                                   assemblyReferences,
                                   projectReferences,
                                   projectTypes,
                                   imports,
                                   sourceFiles,
                                   contentItems,
                                   packages,
                                   packageReferences,
                                   targetFrameworkVersion,
                                   ImmutableList<ProjectFile>.Empty,
                                   buildRoot,
                                   documentationFile);
        }

        private static Package ParseElement(XElement element)
        {
            var id = new PackageId(element.Attribute("id")?.Value ?? string.Empty);
            var version = new PackageVersion(element.Attribute("version")?.Value ?? string.Empty);
            var targetFramework = new PackageTargetFrameworkVersion(element.Attribute("targetFramework")?.Value ?? string.Empty);
            return new Package(id, version, targetFramework);
        }

        private static ImmutableList<ProjectType> AnalyzeProjectTypes(XDocument document)
        {
            var projectTypeGuids = document.ElementBy(ParserHelper.ProjectTypeGuids);
            if (projectTypeGuids.IsNull())
            {
                return new[] { ProjectType.Invalid }.ToImmutableList();
            }

            var result = projectTypeGuids.ValueOrDefault(string.Empty) ?? string.Empty;
            var guidArray = result.Split(';').Select(item => new Guid(item));
            return guidArray.Select(ProjectTypeParser.GetProjectType).ToImmutableList();
        }

        private static ImmutableList<Import> GetAllImports(XDocument document)
        {
            var result = document.ElementsBy(ParserHelper.Import);
            var imports = result.Select(item => new Import(item.AttributeBy(ParserHelper.Project)?.ValueOrDefault() ?? string.Empty))
                .ToImmutableList();
            return imports;
        }

        private static ImmutableList<T> GetReferences<T>(XDocument document, string localName,
                                                          Func<XElement, T> convertFunc) where T : ReferenceBase
        {
            var refrences = document.ElementsBy(localName);
            var result = refrences.Select(convertFunc);
            return result.ToImmutableList();
        }

        /// <summary>
        /// The files the project file itself lists. An old format project enumerates its content, so
        /// the item types that name a file are read rather than the directory being walked.
        /// </summary>
        /// <remarks>
        /// Only files that exist are reported. A stale include used to reach the typed file info
        /// constructor, which throws for a missing path, and took the whole parse with it.
        /// </remarks>
        private static ImmutableList<FileInfo> GetSourceFiles(XDocument document, ProjectFileInfo projectFileInfo)
        {
            var projectDirectory = projectFileInfo.Value.Directory!;

            var itemTypes = new[]
            {
                ParserHelper.Compile, ParserHelper.Page, ParserHelper.Content, ParserHelper.None,
                ParserHelper.Resource, ParserHelper.EmbeddedResource, ParserHelper.ApplicationDefinition
            };

            return itemTypes.SelectMany(document.ElementsBy)
                            .Select(element => element.AttributeBy(ParserHelper.Include)?.ValueOrDefault())
                            .Where(include => include.IsNotNullOrWhiteSpace())
                            .Select(include => new FileInfo(Path.Combine(projectDirectory.FullName, include!)))
                            .Where(file => file.Exists)
                            .DistinctBy(file => file.FullName)
                            .ToImmutableList();
        }

        private static ImmutableList<ProjectContentItem> GetContentItems(XDocument document, ProjectFileInfo projectFileInfo)
        {
            var projectFileDirectoryPath = projectFileInfo.Value.Directory!.FullName;
            var result = document.ElementsBy(ParserHelper.Content);
            var contentItems = result.Select(item =>
            {
                var include = item.AttributeBy(ParserHelper.Include)?.ValueOrDefault(string.Empty);
                var copyToOutputDirectory = item.ElementBy(ParserHelper.CopyToOutputDirectory)?.ValueOrDefault()?.ToCopyToOutputDirectory() ?? CopyToOutputDirectory.DoNotCopy;
                var fileInfo = new FileInfo(Path.Combine(projectFileDirectoryPath, include ?? string.Empty));
                return new ProjectContentItem(include ?? string.Empty, copyToOutputDirectory, fileInfo);
            });

            return contentItems.ToImmutableList();
        }

        private static ImmutableList<Package> GetPackagesFrom(ProjectFileInfo projectFileInfo, XDocument document)
        {
            var packagesConfig = "packages.config";

            var nuGetPackageExists = document.Descendants()
                .Any(d => d.Attributes().Any(a => a.Name.LocalName.EqualsTo("Include") && a.Value.EqualsTo(packagesConfig)));
            if (nuGetPackageExists.IsFalse())
            {
                return ImmutableList<Package>.Empty;
            }

            var path = new FileInfo(Path.Combine(projectFileInfo.Value.Directory!.FullName, packagesConfig));
            if (path.Exists)
            {
                var packageDocument = XDocument.Load(path.FullName);
                return packageDocument.Descendants("package").Select(ParseElement).ToImmutableList();
            }

            return ImmutableList<Package>.Empty;
        }

        private static ImmutableList<PackageReference> GetPackageReferencesFrom(XDocument document)
        {
            var packageReferences = document.ElementsBy("PackageReference");
            return packageReferences.Select(p =>
            {
                var include = p.AttributeBy("Include")?.Value ?? string.Empty;
                var versionAttribute = p.AttributeBy("Version");
                var version = versionAttribute?.Value ?? p.ElementBy("Version")?.Value ?? string.Empty;
                return new PackageReference(include, new PackageVersion(version));
            }).ToImmutableList();
        }
    }
}
