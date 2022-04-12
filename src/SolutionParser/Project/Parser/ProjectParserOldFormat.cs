using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace SolutionParser.Project
{
    internal static class ProjectParserOldFormat
    {
        internal static ProjectFile Parse(ProjectFileInfo projectFileInfo, XDocument document)
        {
            var guid = new Guid(document.ElementBy(ParserHelper.ProjectGuid).ValueOrDefault());
            var projectReferences = GetReferences(document, ParserHelper.ProjectReference, ProjectReferenceParser.Parse).ToList();
            var assemblyReferences = GetReferences(document, ParserHelper.Reference, AssemblyReferenceParser.Parse).ToList();
            var projectTypes = AnalyzeProjectTypes(document).ToList();
            var assemblyName = document.ElementBy(ParserHelper.AssemblyName).ValueOrDefault();
            var imports = GetAllImports(document).ToList();
            var csharpFiles = GetSpecificFiles(document, projectFileInfo, ParserHelper.Compile, ParserHelper.Include,
                ClassCreator.CreateCSharpFile).ToList();
            var xamlFiles = GetSpecificFiles(document, projectFileInfo, ParserHelper.Page, ParserHelper.Include,
                ClassCreator.CreateXAMLFile).ToList();
            var contentItems = GetContentItems(document, projectFileInfo).ToList().ToList();
            var packages = GetPackagesFrom(projectFileInfo, document).ToList();
            var packageReferences = GetPackageReferencesFrom(document).ToList();
            var targetFrameworkVersion = document.ElementBy(ParserHelper.TargetFrameworkVersion).ValueOrDefault().ToIList();
            var buildRoot = document.ElementBy(ParserHelper.BuildRoot).ValueOrDefault();
            var documentationFile = document.ElementBy(ParserHelper.DocumentationFile).ValueOrDefault();

            return new ProjectFile(guid,
                                   document,
                                   projectFileInfo,
                                   assemblyName,
                                   assemblyReferences,
                                   projectReferences,
                                   projectTypes,
                                   imports,
                                   csharpFiles,
                                   xamlFiles,
                                   contentItems,
                                   packages,
                                   packageReferences,
                                   targetFrameworkVersion,
                                   Enumerable.Empty<ProjectFile>(),
                                   buildRoot,
                                   documentationFile);
        }

        private static Package ParseElement(XElement element)
        {
            var id = new PackageId(element.Attribute("id").Value);
            var version = new PackageVersion(element.Attribute("version").Value);
            var targetFramework = new PackageTargetFrameworkVersion(element.Attribute("targetFramework").Value);
            return new Package(id, version, targetFramework);
        }

        private static IEnumerable<ProjectType> AnalyzeProjectTypes(XDocument document)
        {
            var projectTypeGuids = document.ElementBy(ParserHelper.ProjectTypeGuids);
            if (projectTypeGuids == null)
            {
                return new[] { ProjectType.Invalid };
            }

            var result = projectTypeGuids.ValueOrDefault(string.Empty);
            var guidArray = result.Split(';').Select(item => new Guid(item));
            return guidArray.Select(ProjectTypeParser.GetProjectType);
        }

        private static IEnumerable<Import> GetAllImports(XDocument document)
        {
            var result = document.ElementsBy(ParserHelper.Import);
            var imports = result.Select(item => new Import(item.AttributeBy(ParserHelper.Project).ValueOrDefault()))
                .ToList();
            return imports;
        }

        private static IEnumerable<T> GetReferences<T>(XDocument document, string localName,
            Func<XElement, T> convertFunc) where T : ReferenceBase
        {
            var refrences = document.ElementsBy(localName);
            var result = refrences.Select(convertFunc);
            return result;
        }

        private static IEnumerable<T> GetSpecificFiles<T>(XDocument document, ProjectFileInfo projectFileInfo,
            string localName, string attributeName, Func<string, T> creatorFunc)
        {
            var projectDirectoryPath = projectFileInfo.Value.Directory.FullName;
            var result = document.ElementsBy(localName)
                .Select(element => element.AttributeBy(attributeName).ValueOrDefault())
                .Select(item => Path.Combine(projectDirectoryPath, item))
                .Select(creatorFunc);
            return result;
        }

        private static IEnumerable<ProjectContentItem> GetContentItems(XDocument document, ProjectFileInfo projectFileInfo)
        {
            var projectFileDirectoryPath = projectFileInfo.Value.Directory.FullName;
            var result = document.ElementsBy(ParserHelper.Content);
            var contentItems = result.Select(
                item =>
                {
                    var include = item.AttributeBy(ParserHelper.Include).ValueOrDefault();
                    var copyToOutputDirectory = item.ElementBy(ParserHelper.CopyToOutputDirectory).ValueOrDefault()
                        .ToCopyToOutputDirectory();
                    var fileInfo = new FileInfo(Path.Combine(projectFileDirectoryPath, include));
                    return new ProjectContentItem(include, copyToOutputDirectory, fileInfo);
                });

            return contentItems;
        }

        private static IEnumerable<Package> GetPackagesFrom(ProjectFileInfo projectFileInfo, XDocument document)
        {
            var packagesConfig = "packages.config";

            var nuGetPackageExists = document.Descendants()
                .Any(d => d.Attributes().Any(a => a.Name.LocalName == "Include" && a.Value == packagesConfig));
            if (nuGetPackageExists.IsFalse())
            {
                return Enumerable.Empty<Package>();
            }

            var path = new FileInfo(Path.Combine(projectFileInfo.Value.Directory.FullName, packagesConfig));
            if (path.Exists)
            {
                var packageDocument = XDocument.Load(path.FullName);
                return packageDocument.Descendants("package").Select(ParseElement).ToList();
            }

            return Enumerable.Empty<Package>();
        }

        private static IEnumerable<PackageReference> GetPackageReferencesFrom(XDocument document)
        {
            var packageReferences = document.ElementsBy("PackageReference");
            if (packageReferences.IsEmpty())
            {
                return Enumerable.Empty<PackageReference>();
            }

            return packageReferences.Select(p =>
            {
                var include = p.AttributeBy("Include").Value;
                var versionAttribute = p.AttributeBy("Version");
                var version = versionAttribute == null ? p.ElementBy("Version").Value : versionAttribute.Value;
                return new PackageReference(include, new PackageVersion(version));
            });
        }
    }
}
