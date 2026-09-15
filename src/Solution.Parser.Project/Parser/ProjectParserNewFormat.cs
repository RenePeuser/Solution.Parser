using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            "MSTest.", "Microsoft.NET.Test.Sdk", "NUnit", "XUnit"
        };

        internal static ProjectFile Parse(ProjectFileInfo projectFileInfo, XDocument document)
        {
            var projectReferences = GetReferences(document, ParserHelper.ProjectReference, ProjectReferenceParser.Parse);

            var guid = Guid.Empty;

            var packages = GetPackagesFrom(projectFileInfo);
            var packageReferences = GetPackageReferencesFrom(document);

            var sourceFiles = GetSourceFiles(projectFileInfo);
            var projectTypes = AnalyzeProjectTypes(document, packageReferences).ToImmutableList();

            var contentItems = ImmutableList<ProjectContentItem>.Empty;
            var assemblyReferences = ImmutableList<AssemblyReference>.Empty;

            var assemblyName = document.ElementBy(ParserHelper.AssemblyName)?.ValueOrDefault() ?? projectFileInfo.FileNameWithoutExtenion;

            var imports = GetAllImports(document);
            var targetFrameworkVersion = document.ElementBy(ParserHelper.TargetFrameworkNewFormat)?.ValueOrDefault();
            var targetFrameworkVersions = document.ElementBy(ParserHelper.TargetFrameworksNewFormat)?.ValueOrDefault()?.Split(';').ToImmutableList() ?? ImmutableList<string>.Empty;

            var targetVersions = targetFrameworkVersion.IsNotNull() ? targetFrameworkVersion.ToIList().ToImmutableList() : targetFrameworkVersions;

            var buildRoot = document.ElementBy(ParserHelper.BuildRoot)?.ValueOrDefault() ?? string.Empty;
            var documentationFile = document.ElementBy(ParserHelper.DocumentationFile)?.ValueOrDefault() ?? string.Empty;

            return new ProjectFile(guid, document, projectFileInfo, assemblyName, assemblyReferences,
                projectReferences,
                projectTypes, imports, sourceFiles, contentItems, packages, packageReferences,
                targetVersions, ImmutableList<ProjectFile>.Empty, buildRoot, documentationFile);
        }

        private static Package ParseElement(XElement element)
        {
            var id = new PackageId(element.Attribute("id")?.Value ?? string.Empty);
            var version = new PackageVersion(element.Attribute("version")?.Value ?? string.Empty);
            var targetFramework = new PackageTargetFrameworkVersion(element.Attribute("targetFramework")?.Value ?? string.Empty);
            return new Package(id, version, targetFramework);
        }

        private static IEnumerable<ProjectType> AnalyzeProjectTypes(XDocument document,
                                                                    ImmutableList<PackageReference> packageReferences)
        {
            var isTestProject = document.ElementBy(ParserHelper.IsTestProject)?.ValueOrDefault() ?? string.Empty;

            if (isTestProject.Contains("true", StringComparison.OrdinalIgnoreCase))
            {
                yield return ProjectType.Test;
            } 
            else if (packageReferences.Any(package => sTestPackages.Any(testPackage => package.Include.Contains(testPackage))))
            {
                yield return ProjectType.Test;

            }

            yield return ProjectType.C_Sharp;
        }

        private static ImmutableList<Import> GetAllImports(XDocument document)
        {
            var result = document.ElementsBy(ParserHelper.Import);
            var imports = result.Select(item => new Import(item.AttributeBy(ParserHelper.Project)?.ValueOrDefault() ?? string.Empty))
                .ToImmutableList();
            return imports;
        }

        private static ImmutableList<T> GetReferences<T>(XDocument document, string localName, Func<XElement, T> convertFunc) where T : ReferenceBase
        {
            var refrences = document.ElementsBy(localName);
            var result = refrences.Select(convertFunc);
            return result.ToImmutableList();
        }

        /// <summary>
        /// Every file below the project directory, build output excluded. The directory is walked
        /// once; it used to be walked once per file type, and the typed lists it produced forced this
        /// package to depend on both language packages.
        /// </summary>
        private static ImmutableList<FileInfo> GetSourceFiles(ProjectFileInfo projectFileInfo)
        {
            var projectDirectory = projectFileInfo.Value.Directory!;

            return projectDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                                   .Where(file => SourceFileFilter.IsSourceFile(file, projectDirectory))
                                   .ToImmutableList();
        }

        private static ImmutableList<Package> GetPackagesFrom(ProjectFileInfo projectFileInfo)
        {
            var packagesConfigFileInfo = new FileInfo(Path.Combine(projectFileInfo.Value.Directory!.FullName, "packages.config"));
            if (packagesConfigFileInfo.Exists.IsFalse())
            {
                return ImmutableList<Package>.Empty;
            }

            var packageDocument = XDocument.Load(packagesConfigFileInfo.FullName);
            return packageDocument.Descendants("package").Select(ParseElement).ToImmutableList();
        }

        private static ImmutableList<PackageReference> GetPackageReferencesFrom(XDocument document)
        {
            var packageReferences = document.ElementsBy("PackageReference");
            if (packageReferences.IsEmpty())
            {
                return ImmutableList<PackageReference>.Empty;
            }

            return packageReferences.Select(p =>
            {
                var include = p.AttributeBy("Include")?.Value is null ? p.AttributeBy("Update")?.Value : p.AttributeBy("Include")?.Value;
                var versionAttribute = p.AttributeBy("Version");
                var version = versionAttribute?.Value ?? p.ElementBy("Version")?.Value;
                return new PackageReference(include ?? string.Empty, new PackageVersion(version ?? string.Empty));
            }).ToImmutableList();
        }
    }
}
