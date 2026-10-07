using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solution.Parser.CSharp
{
    /// <summary>A built compilation and the trees inside it, by file path.</summary>
    internal sealed record ProjectCompilation(CSharpCompilation Compilation, ImmutableDictionary<string, SyntaxTree> TreesByPath);

    /// <summary>
    /// The caches behind a <see cref="CodeBase"/>. Everything is lazy: a file is parsed the first time
    /// someone asks for it, a project is compiled the first time a symbol of it is asked for, and
    /// neither happens twice.
    /// </summary>
    internal sealed class Workspace
    {
        internal static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private static readonly ConcurrentDictionary<string, MetadataReference> MetadataReferences = new(PathComparer);

        private readonly ImmutableDictionary<string, string> _inMemorySources;
        private readonly ConcurrentDictionary<string, Lazy<SyntaxTree>> _syntaxTrees = new(PathComparer);
        private readonly ConcurrentDictionary<string, Lazy<CSharpSyntaxTree>> _models = new(PathComparer);
        private readonly ConcurrentDictionary<string, Lazy<ProjectCompilation>> _compilations = new(PathComparer);
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModels = new();
        private readonly ConcurrentDictionary<string, PackageAssembly> _packagesByPath = new(PathComparer);
        private readonly ImmutableHashSet<string> _sourceFiles;
        private readonly ImmutableDictionary<string, string> _projectKeyByFile;
        private int _compilationCount;
        private volatile bool _hasSymbols;

        internal Workspace(ImmutableList<ProjectInput> projects, ImmutableDictionary<string, string> inMemorySources)
        {
            Projects = projects;
            ProjectsByKey = projects.ToImmutableDictionary(p => p.Key, PathComparer);
            _inMemorySources = inMemorySources.WithComparers(PathComparer);
            _sourceFiles = projects.SelectMany(p => p.SourceFiles).ToImmutableHashSet(PathComparer);

            // A file linked into two projects belongs to the first one that lists it.
            _projectKeyByFile = projects.SelectMany(p => p.SourceFiles.Select(f => (File: f, p.Key)))
                                        .DistinctBy(e => e.File, PathComparer)
                                        .ToImmutableDictionary(e => e.File, e => e.Key, PathComparer);
        }

        internal ImmutableList<ProjectInput> Projects { get; }

        internal ImmutableDictionary<string, ProjectInput> ProjectsByKey { get; }

        /// <summary>How many compilations were built, so a test can prove the fast mode builds none.</summary>
        internal int CompilationCount => Volatile.Read(ref _compilationCount);

        /// <summary>True for a file a rule sees, as opposed to one the build generated into obj.</summary>
        internal bool IsSourceFile(string path)
        {
            return _sourceFiles.Contains(path);
        }

        /// <summary>True once <see cref="EnableSymbols"/> ran; the calls of every tree are then known to the registry.</summary>
        internal bool HasSymbols => _hasSymbols;

        internal CSharpSyntaxTree Model(string path)
        {
            return _models.GetOrAdd(path, p => new Lazy<CSharpSyntaxTree>(() =>
                                                {
                                                    var model = SyntaxTree(p).Parse(p);

                                                    if (_hasSymbols)
                                                    {
                                                        Register(p, model);
                                                    }

                                                    return model;
                                                })).Value;
        }

        /// <summary>
        /// Makes the calls of this workspace resolvable: those of the trees parsed so far right away, the
        /// others as they are parsed.
        /// </summary>
        /// <remarks>
        /// The flag goes first, so a tree is either parsed after it and registers itself, or is already in
        /// the cache and registered here; reading the value waits for a parse in flight.
        /// </remarks>
        internal void EnableSymbols()
        {
            if (_hasSymbols)
            {
                return;
            }

            _hasSymbols = true;

            foreach (var (path, model) in _models)
            {
                Register(path, model.Value);
            }
        }

        internal ProjectCompilation Compilation(string projectKey)
        {
            return _compilations.GetOrAdd(projectKey, key => new Lazy<ProjectCompilation>(() => Build(ProjectsByKey[key]))).Value;
        }

        internal SemanticModel SemanticModel(SyntaxTree tree, CSharpCompilation compilation)
        {
            return _semanticModels.GetOrAdd(tree, t => compilation.GetSemanticModel(t));
        }

        internal PackageAssembly? PackageOf(string assemblyPath)
        {
            return _packagesByPath.TryGetValue(assemblyPath, out var package) ? package : null;
        }

        private void Register(string path, CSharpSyntaxTree model)
        {
            if (_projectKeyByFile.TryGetValue(path, out var projectKey))
            {
                SemanticRegistry.Register(model, this, projectKey);
            }
        }

        /// <summary>
        /// The tree a file was parsed into. Parsed the way <c>CSharpParser.Parse(CSharpFileInfo)</c>
        /// does, so both modes see the same spans, but with the path set so Roslyn can point back at it.
        /// </summary>
        private SyntaxTree SyntaxTree(string path)
        {
            return _syntaxTrees.GetOrAdd(path,
                                         p => new Lazy<SyntaxTree>(() =>
                                         {
                                             var code = _inMemorySources.TryGetValue(p, out var source) ? source : File.ReadAllText(p);

                                             return Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code, path: p);
                                         })).Value;
        }

        private ProjectCompilation Build(ProjectInput project)
        {
            Interlocked.Increment(ref _compilationCount);

            // The roots are shared with the fast mode trees, only the options change. No second parse.
            var trees = project.SourceFiles
                               .Concat(project.GeneratedFiles)
                               .Distinct(PathComparer)
                               .Select(path => SyntaxTree(path))
                               .Select(tree => tree.WithRootAndOptions(tree.GetRoot(), project.ParseOptions))
                               .ToList();

            if (project.NeedsDefaultImplicitUsings)
            {
                trees.Add(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(ProjectInput.DefaultImplicitUsings,
                                                                                   project.ParseOptions,
                                                                                   project.Key + ".ImplicitUsings.g.cs"));
            }

            foreach (var package in project.Packages)
            {
                _packagesByPath.TryAdd(package.Path, package);
            }

            var references = project.FrameworkAssemblies
                                    .Concat(project.Packages.Select(p => p.Path))
                                    .Distinct(PathComparer)
                                    .Select(path => MetadataReferences.GetOrAdd(path, p => MetadataReference.CreateFromFile(p)))
                                    .Concat(TransitiveProjectReferences(project).Select(key => Compilation(key).Compilation.ToMetadataReference()));

            var compilation = SourceGenerators.Run(CSharpCompilation.Create(project.AssemblyName, trees, references, project.CompilationOptions),
                                                   project.Analyzers,
                                                   project.ParseOptions);

            return new ProjectCompilation(compilation, trees.ToImmutableDictionary(t => t.FilePath, PathComparer));
        }

        /// <summary>
        /// The referenced projects and the ones they reference in turn, as MSBuild flows them: a test
        /// project that references Sln sees Project too. A project outside the solution is skipped.
        /// </summary>
        private ImmutableList<string> TransitiveProjectReferences(ProjectInput project)
        {
            var seen = ImmutableHashSet.CreateBuilder<string>(PathComparer);
            var pending = new System.Collections.Generic.Stack<string>(project.ProjectReferenceKeys);

            while (pending.Count > 0)
            {
                var key = pending.Pop();

                if (!ProjectsByKey.TryGetValue(key, out var referenced) || !seen.Add(key))
                {
                    continue;
                }

                foreach (var next in referenced.ProjectReferenceKeys)
                {
                    pending.Push(next);
                }
            }

            return seen.ToImmutableList();
        }
    }
}
