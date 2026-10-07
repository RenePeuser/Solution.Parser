using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Runs the source generators the real build runs, so that a <c>[GeneratedRegex]</c> or an
    /// <c>[ObservableProperty]</c> has its other half and calls into generated code resolve.
    /// </summary>
    /// <remarks>
    /// The generated files are compiled but never reported as trees: a rule judges the code people wrote.
    /// A generator that fails to load or to run is skipped; the compilation then misses its output,
    /// exactly as it did before generators were run at all.
    /// </remarks>
    internal static class SourceGenerators
    {
        private static readonly ConcurrentDictionary<string, ImmutableList<ISourceGenerator>> Loaded = new(Workspace.PathComparer);

        private static readonly IAnalyzerAssemblyLoader Loader = new LoadFromLoader();

        internal static CSharpCompilation Run(CSharpCompilation compilation, ImmutableList<string> analyzerPaths, CSharpParseOptions parseOptions)
        {
            var generators = analyzerPaths.SelectMany(path => Loaded.GetOrAdd(path, Load)).ToImmutableList();

            if (generators.IsEmpty)
            {
                return compilation;
            }

            try
            {
                CSharpGeneratorDriver.Create(generators, parseOptions: parseOptions)
                                     .RunGeneratorsAndUpdateCompilation(compilation, out var generated, out _);

                return (CSharpCompilation)generated;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                return compilation;
            }
        }

        /// <summary>
        /// The analyzer assemblies a package brings for C#. A package that ships one folder per Roslyn
        /// version gets the newest folder this Roslyn can load.
        /// </summary>
        internal static ImmutableList<string> OfPackage(string packageDirectory, IEnumerable<string> files)
        {
            var analyzers = files.Where(f => f.StartsWith("analyzers/dotnet/", StringComparison.OrdinalIgnoreCase)
                                             && f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                             && !f.Contains("/vb/", StringComparison.OrdinalIgnoreCase))
                                 .Select(f => (File: f, Roslyn: RoslynVersionOf(f)))
                                 .ToList();

            var roslyn = typeof(CSharpCompilation).Assembly.GetName().Version ?? new Version(0, 0);
            var bestRoslyn = analyzers.Select(a => a.Roslyn)
                                      .Where(v => v is not null && v <= roslyn)
                                      .Max();

            return analyzers.Where(a => a.Roslyn is null || a.Roslyn == bestRoslyn)
                            .Select(a => Path.GetFullPath(Path.Combine(packageDirectory, a.File)))
                            .Where(File.Exists)
                            .ToImmutableList();
        }

        private static Version? RoslynVersionOf(string file)
        {
            var folder = file.Split('/').FirstOrDefault(s => s.StartsWith("roslyn", StringComparison.OrdinalIgnoreCase));

            return folder is not null && Version.TryParse(folder["roslyn".Length..], out var version) ? version : null;
        }

        private static ImmutableList<ISourceGenerator> Load(string path)
        {
            try
            {
                return new AnalyzerFileReference(path, Loader).GetGenerators(LanguageNames.CSharp).ToImmutableList();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                return ImmutableList<ISourceGenerator>.Empty;
            }
        }

        /// <summary>Loads next to the analyzer, so an analyzer finds its own dependencies in its folder.</summary>
        private sealed class LoadFromLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}
