using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class ProjectParserNewFormat
    {
        private static readonly string[] sTestPackages =
        {
            "MSTest.TestAdapter", "MsTest.TestFramework", "Microsoft.NET.Test.Sdk", "NUnit"
        };

        internal static ProjectFile Parse(ProjectFileInfo projectFileInfo, XDocument document)
        {
            var projectReferences = GetReferences(document, ParserHelper.ProjectReference, ProjectReferenceParser.Parse).ToList();

            var guid = Guid.Empty;

            var packages = GetPackagesFrom(projectFileInfo).ToList();
            var packageReferences = GetPackageReferencesFrom(document).ToList();

            var csharpFiles = GetSpecificFiles(projectFileInfo, FileFilterFunc.CSharpFileInfoFilterFunc, ClassCreator.CreateCSharpFile).ToList();
            var xamlFiles = GetSpecificFiles(projectFileInfo, FileFilterFunc.XamlFileInfoFilterFunc, ClassCreator.CreateXAMLFile).ToList();
            var projectTypes = AnalyzeProjectTypes(packageReferences).ToList();

            var contentItems = Enumerable.Empty<ProjectContentItem>();
            var assemblyReferences = Enumerable.Empty<AssemblyReference>();

            var assemblyName = document.ElementBy(ParserHelper.AssemblyName).ValueOrDefault();

            var imports = GetAllImports(document).ToList();
            var targetFrameworkVersion = document.ElementBy(ParserHelper.TargetFrameworkNewFormat).ValueOrDefault();
            var targetFrameworkVersions = document.ElementBy(ParserHelper.TargetFrameworksNewFormat)?.ValueOrDefault()?.Split(';').ToList();

            var targetVersions = targetFrameworkVersion.IsNotNull() ? targetFrameworkVersion.ToIList() : targetFrameworkVersions;

            var buildRoot = document.ElementBy(ParserHelper.BuildRoot).ValueOrDefault();
            var documentationFile = document.ElementBy(ParserHelper.DocumentationFile).ValueOrDefault();

            return new ProjectFile(guid, document, projectFileInfo, assemblyName, assemblyReferences,
                projectReferences,
                projectTypes, imports, csharpFiles, xamlFiles, contentItems, packages, packageReferences,
                targetVersions, Enumerable.Empty<ProjectFile>(), buildRoot, documentationFile);
        }

        private static Package ParseElement(XElement element)
        {
            var id = new PackageId(element.Attribute("id").Value);
            var version = new PackageVersion(element.Attribute("version").Value);
            var targetFramework = new PackageTargetFrameworkVersion(element.Attribute("targetFramework").Value);
            return new Package(id, version, targetFramework);
        }

        private static IEnumerable<ProjectType> AnalyzeProjectTypes(IEnumerable<PackageReference> packageReferences)
        {
            if (packageReferences.Any(package => sTestPackages.Any(testPackage => testPackage == package.Include)))
            {
                yield return ProjectType.Test;
            }

            yield return ProjectType.C_Sharp;
        }

        private static IEnumerable<Import> GetAllImports(XDocument document)
        {
            var result = document.ElementsBy(ParserHelper.Import);
            var imports = result.Select(item => new Import(item.AttributeBy(ParserHelper.Project).ValueOrDefault()))
                .ToList();
            return imports;
        }

        private static IEnumerable<T> GetReferences<T>(XDocument document, string localName, Func<XElement, T> convertFunc) where T : ReferenceBase
        {
            var refrences = document.ElementsBy(localName);
            var result = refrences.Select(convertFunc);
            return result;
        }

        private static IEnumerable<T> GetSpecificFiles<T>(ProjectFileInfo projectFileInfo, Func<FileInfo, bool> filterFunc, Func<FileInfo, T> creatorFunc)
        {
            var directoriesToIgnore = new[] { "bin", "obj" };

            var directoriesToEnumerateForFiles = projectFileInfo.Value.Directory.EnumerateDirectories().Where(directory => !directoriesToIgnore.Contains(directory.Name)).ToList();

            foreach (var directoriesToEnumerateForFile in directoriesToEnumerateForFiles)
            {
                var expectedFileInfos = directoriesToEnumerateForFile.EnumerateFiles("*.*", SearchOption.AllDirectories).Where(filterFunc).ToList();
                foreach (var expectedFileInfo in expectedFileInfos)
                {
                    yield return creatorFunc(expectedFileInfo);
                }
            }
        }

        private static IEnumerable<Package> GetPackagesFrom(ProjectFileInfo projectFileInfo)
        {
            var packagesConfigFileInfo =
                new FileInfo(Path.Combine(projectFileInfo.Value.Directory.FullName, "packages.config"));
            if (packagesConfigFileInfo.Exists.IsFalse())
            {
                return Enumerable.Empty<Package>();
            }

            var packageDocument = XDocument.Load(packagesConfigFileInfo.FullName);
            return packageDocument.Descendants("package").Select(ParseElement).ToList();
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
                var version = versionAttribute == null
                    ? p.ElementBy("Version").Value
                    : versionAttribute.Value;
                return new PackageReference(include, new PackageVersion(version));
            });
        }
    }
}
