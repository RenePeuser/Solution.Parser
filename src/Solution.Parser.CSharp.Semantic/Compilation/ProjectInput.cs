using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Solution.Parser.Project;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Everything a compilation of one project is made of, read up front so that building it later is
    /// nothing but handing it to Roslyn.
    /// </summary>
    /// <param name="Key">The full path of the csproj, or a made up name for code given in memory.</param>
    /// <param name="AssemblyName">The name of the compiled assembly.</param>
    /// <param name="SourceFiles">The files the user sees as the project's code, the ones a rule walks.</param>
    /// <param name="GeneratedFiles">Files the build wrote to obj, like the implicit global usings; compiled, never reported.</param>
    /// <param name="NeedsDefaultImplicitUsings">The csproj enables implicit usings but no build wrote them yet.</param>
    /// <param name="ProjectReferenceKeys">The full paths of the referenced csproj files.</param>
    /// <param name="Packages">The package assemblies restore resolved, transitive ones included.</param>
    /// <param name="FrameworkAssemblies">The reference assemblies of the shared frameworks.</param>
    /// <param name="Analyzers">The analyzer assemblies of frameworks and packages, run for their source generators.</param>
    /// <param name="ParseOptions">The language version to bind with.</param>
    /// <param name="CompilationOptions">Nullable context and unsafe code as the csproj sets them.</param>
    internal sealed record ProjectInput(string Key,
                                        string AssemblyName,
                                        ImmutableList<string> SourceFiles,
                                        ImmutableList<string> GeneratedFiles,
                                        bool NeedsDefaultImplicitUsings,
                                        ImmutableList<string> ProjectReferenceKeys,
                                        ImmutableList<PackageAssembly> Packages,
                                        ImmutableList<string> FrameworkAssemblies,
                                        ImmutableList<string> Analyzers,
                                        CSharpParseOptions ParseOptions,
                                        CSharpCompilationOptions CompilationOptions)
    {
        /// <summary>The usings the SDK adds for <c>&lt;ImplicitUsings&gt;enable&lt;/ImplicitUsings&gt;</c>, for a project that was never built.</summary>
        internal const string DefaultImplicitUsings = """
            global using global::System;
            global using global::System.Collections.Generic;
            global using global::System.IO;
            global using global::System.Linq;
            global using global::System.Net.Http;
            global using global::System.Threading;
            global using global::System.Threading.Tasks;
            """;

        internal static ProjectInput From(ProjectFile project)
        {
            var projectPath = project.ProjectFileInfo.Value.FullName;
            var projectDirectory = Path.GetDirectoryName(projectPath)!;
            var document = project.Document;
            var assets = AssetsFile.TryRead(projectDirectory);

            var targetFramework = assets?.TargetFramework ?? project.TargetFrameworkVersion.FirstOrDefault();
            var frameworkReferences = assets?.FrameworkReferences ?? ImmutableList<string>.Empty;

            var projectReferences = project.ProjectReferences
                                           .Where(r => !string.IsNullOrWhiteSpace(r.Include))
                                           .Select(r => Path.GetFullPath(Path.Combine(projectDirectory, r.Include.Replace('\\', Path.DirectorySeparatorChar))))
                                           .ToImmutableList();

            var nullable = Property(document, "Nullable");
            var allowUnsafe = Property(document, "AllowUnsafeBlocks");

            var generatedFiles = GeneratedFilesOf(projectDirectory);

            return new ProjectInput(projectPath,
                                    project.AssemblyName,
                                    project.SourceFiles.GetCSharpFiles().Select(f => f.Value.FullName).ToImmutableList(),
                                    generatedFiles,
                                    !generatedFiles.Any(f => f.EndsWith(".GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase)) && HasImplicitUsings(document),
                                    projectReferences,
                                    assets?.Packages ?? ImmutableList<PackageAssembly>.Empty,
                                    ReferencePacks.For(targetFramework, frameworkReferences),
                                    ReferencePacks.AnalyzersFor(targetFramework, frameworkReferences)
                                                  .AddRange(assets?.Analyzers ?? ImmutableList<string>.Empty),
                                    ParseOptionsFor(Property(document, "LangVersion")),
                                    CompilationOptionsFor(nullable, allowUnsafe));
        }

        /// <summary>A single project of code given in memory, compiled against the running runtime.</summary>
        internal static ProjectInput InMemory(string key, ImmutableList<string> sourceFiles, ImmutableList<string> generatedFiles)
        {
            return new ProjectInput(key,
                                    key,
                                    sourceFiles,
                                    generatedFiles,
                                    NeedsDefaultImplicitUsings: false,
                                    ImmutableList<string>.Empty,
                                    ImmutableList<PackageAssembly>.Empty,
                                    ReferencePacks.RunningRuntime(),
                                    ImmutableList<string>.Empty,
                                    ParseOptionsFor(null),
                                    CompilationOptionsFor("enable", null));
        }

        /// <summary>
        /// What the build generated into obj and the compiler needs: the implicit global usings, the
        /// assembly attributes (among them the <c>InternalsVisibleTo</c> a csproj item declares) and the
        /// <c>*.g.cs</c> files of build steps like the XAML compiler, which hold <c>InitializeComponent</c>.
        /// Taking the folder of the newest one covers Debug and Release and several target frameworks
        /// without guessing which one is meant.
        /// </summary>
        private static ImmutableList<string> GeneratedFilesOf(string projectDirectory)
        {
            var obj = Path.Combine(projectDirectory, "obj");

            if (!Directory.Exists(obj))
            {
                return ImmutableList<string>.Empty;
            }

            string[] patterns = ["*.GlobalUsings.g.cs", "*.AssemblyInfo.cs"];

            var newest = patterns.SelectMany(p => Directory.EnumerateFiles(obj, p, SearchOption.AllDirectories))
                                 .Select(f => new FileInfo(f))
                                 .OrderByDescending(f => f.LastWriteTimeUtc)
                                 .FirstOrDefault();

            if (newest?.DirectoryName is null)
            {
                return ImmutableList<string>.Empty;
            }

            var generated = Directory.EnumerateFiles(newest.DirectoryName, "*.g.cs", SearchOption.AllDirectories)
                                     .Where(f => !IsIntermediate(f, newest.DirectoryName));

            return patterns.SelectMany(p => Directory.EnumerateFiles(newest.DirectoryName, p, SearchOption.TopDirectoryOnly))
                           .Where(f => !IsIntermediate(f, newest.DirectoryName))
                           .Concat(generated)
                           .Select(Path.GetFullPath)
                           .Distinct(Workspace.PathComparer)
                           .ToImmutableList();
        }

        /// <summary>
        /// Files that would compile twice: the temporary project the WPF build compiles first, the
        /// IntelliSense copies of the XAML output and source generator output written to disk, which the
        /// generators produce again in memory.
        /// </summary>
        private static bool IsIntermediate(string file, string outputDirectory)
        {
            var relative = Path.GetRelativePath(outputDirectory, file);

            return relative.Contains("_wpftmp", StringComparison.OrdinalIgnoreCase)
                   || relative.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
                   || relative.StartsWith("generated" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasImplicitUsings(XDocument document)
        {
            var value = Property(document, "ImplicitUsings");

            return string.Equals(value, "enable", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The compilation reuses the roots the syntax trees were parsed with, so a finding points at
        /// the same span in both modes. That also means preprocessor symbols cannot change anything any
        /// more, only the language version is taken over. Preview when unset, so binding never fails on a
        /// feature the real build has switched on in a Directory.Build.props this reader does not see.
        /// </summary>
        private static CSharpParseOptions ParseOptionsFor(string? langVersion)
        {
            var version = LanguageVersionFacts.TryParse(langVersion, out var parsed) ? parsed : LanguageVersion.Preview;

            return new CSharpParseOptions(version, DocumentationMode.Parse);
        }

        private static CSharpCompilationOptions CompilationOptionsFor(string? nullable, string? allowUnsafe)
        {
            var nullableContext = nullable?.ToLowerInvariant() switch
            {
                "disable" => NullableContextOptions.Disable,
                "warnings" => NullableContextOptions.Warnings,
                "annotations" => NullableContextOptions.Annotations,
                _ => NullableContextOptions.Enable
            };

            return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                nullableContextOptions: nullableContext,
                                                allowUnsafe: string.Equals(allowUnsafe, "true", StringComparison.OrdinalIgnoreCase),
                                                metadataImportOptions: MetadataImportOptions.Public);
        }

        /// <summary>The last value of an MSBuild property in the csproj itself; Directory.Build.props is not evaluated.</summary>
        private static string? Property(XDocument document, string name)
        {
            return document.Descendants()
                           .Where(e => e.Name.LocalName == name && e.Parent?.Name.LocalName == "PropertyGroup")
                           .Select(e => e.Value.Trim())
                           .LastOrDefault(v => v.Length > 0);
        }
    }
}
