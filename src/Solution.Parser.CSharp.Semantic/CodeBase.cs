using System;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Solution.Parser.Project;
using Solution.Parser.Sln;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The entry point for code rules over a whole solution, in two modes:
    /// <list type="bullet">
    /// <item><c>CodeBase.Open(solution)</c> is the fast mode: syntax trees only, exactly what
    /// <c>CSharpFileInfo.Parse()</c> gives, but parsed once and cached.</item>
    /// <item><c>.WithSymbols()</c> adds the full mode on top: <c>call.Method</c>,
    /// <c>call.Argument("writeResponse")</c> and the like answer from a Roslyn compilation per
    /// project, built the first time a symbol of that project is asked for.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Both modes share the caches, so asking for symbols later never parses a file again. A symbol
    /// access in the fast mode throws instead of guessing, which keeps the cost of a rule visible.
    /// </remarks>
    public sealed class CodeBase
    {
        private readonly Workspace _workspace;

        private CodeBase(Workspace workspace, SolutionFile? solution, bool hasSymbols)
        {
            _workspace = workspace;
            Solution = solution;
            HasSymbols = hasSymbols;
        }

        /// <summary>The parsed solution, null for code given with <see cref="FromSources"/>.</summary>
        public SolutionFile? Solution { get; }

        /// <summary>True after <see cref="WithSymbols"/>.</summary>
        public bool HasSymbols { get; }

        /// <summary>Every syntax tree of every project, a file linked into two projects once.</summary>
        public ImmutableList<CSharpSyntaxTree> AllTrees => TreesOf(_workspace.Projects);

        public ImmutableList<CSharpSyntaxTree> ProductiveTrees => Solution is null ? AllTrees : Trees(Solution.ProductiveProjects);

        public ImmutableList<CSharpSyntaxTree> UnitTestTrees => Solution is null ? ImmutableList<CSharpSyntaxTree>.Empty : Trees(Solution.UnitTestProjects);

        internal Workspace Workspace => _workspace;

        public static CodeBase Open(SolutionFileInfo solutionFileInfo)
        {
            Throw.IfNull(solutionFileInfo);

            return Open(solutionFileInfo.Parse());
        }

        public static CodeBase Open(SolutionFile solution)
        {
            Throw.IfNull(solution);

            var projects = solution.Projects.Select(ProjectInput.From).ToImmutableList();

            return new CodeBase(new Workspace(projects, ImmutableDictionary<string, string>.Empty), solution, hasSymbols: false);
        }

        /// <summary>
        /// Code given in memory, compiled as one project against the running runtime. Meant for tests
        /// and for trying a rule out; the file names only have to be unique.
        /// </summary>
        public static CodeBase FromSources(params (string FileName, string Code)[] sources)
        {
            Throw.IfNull(sources);

            if (sources.Length == 0)
            {
                throw new ArgumentException("Give at least one source file.", nameof(sources));
            }

            var files = sources.ToImmutableDictionary(s => s.FileName, s => s.Code, Workspace.PathComparer);
            var project = ProjectInput.InMemory("InMemory", files.Keys.ToImmutableList(), ImmutableList<string>.Empty);

            return new CodeBase(new Workspace(ImmutableList.Create(project), files), solution: null, hasSymbols: false);
        }

        /// <summary>
        /// The same code base with symbols. Nothing is compiled yet; each project is compiled the first
        /// time one of its symbols is asked for.
        /// </summary>
        public CodeBase WithSymbols()
        {
            if (HasSymbols)
            {
                return this;
            }

            SemanticRegistry.Register(_workspace);

            return new CodeBase(_workspace, Solution, hasSymbols: true);
        }

        public ImmutableList<CSharpSyntaxTree> Trees(ProjectFile project)
        {
            Throw.IfNull(project);

            return Trees([project]);
        }

        public ImmutableList<CSharpSyntaxTree> Trees(ImmutableList<ProjectFile> projects)
        {
            Throw.IfNull(projects);

            var keys = projects.Select(p => p.ProjectFileInfo.Value.FullName).ToImmutableHashSet(Workspace.PathComparer);

            return TreesOf(_workspace.Projects.Where(p => keys.Contains(p.Key)));
        }

        /// <summary>
        /// What the compiler reports for the project. Errors here mean symbols may be missing, for
        /// example because the solution was not restored; a rule can assert on that before trusting
        /// <c>Unresolved</c>.
        /// </summary>
        public ImmutableList<ParseDiagnostic> CompilationDiagnostics(ProjectFile project)
        {
            Throw.IfNull(project);

            return Diagnostics(project.ProjectFileInfo.Value.FullName);
        }

        /// <summary>What the compiler reports for every project.</summary>
        public ImmutableList<ParseDiagnostic> CompilationDiagnostics()
        {
            return _workspace.Projects.SelectMany(p => Diagnostics(p.Key)).ToImmutableList();
        }

        private ImmutableList<ParseDiagnostic> Diagnostics(string projectKey)
        {
            if (!HasSymbols)
            {
                throw SymbolsNotLoaded.Exception("CompilationDiagnostics()");
            }

            return _workspace.Compilation(projectKey)
                             .Compilation
                             .GetDiagnostics()
                             .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                             .Select(ToParseDiagnostic)
                             .ToImmutableList();
        }

        private ImmutableList<CSharpSyntaxTree> TreesOf(System.Collections.Generic.IEnumerable<ProjectInput> projects)
        {
            return projects.SelectMany(p => p.SourceFiles)
                           .Distinct(Workspace.PathComparer)
                           .Select(_workspace.Model)
                           .ToImmutableList();
        }

        private static ParseDiagnostic ToParseDiagnostic(Diagnostic diagnostic)
        {
            var span = diagnostic.Location.GetLineSpan();
            var location = diagnostic.Location.IsInSource
                               ? new CodeLocation(span.Path,
                                                  span.StartLinePosition.Line + 1,
                                                  span.StartLinePosition.Character + 1,
                                                  span.EndLinePosition.Line + 1,
                                                  span.EndLinePosition.Character + 1,
                                                  diagnostic.Location.SourceSpan.Start,
                                                  diagnostic.Location.SourceSpan.Length)
                               : CodeLocation.None;

            return new ParseDiagnostic(diagnostic.Id, diagnostic.Severity, diagnostic.GetMessage(), location);
        }
    }

    internal static class SymbolsNotLoaded
    {
        internal static InvalidOperationException Exception(string member)
        {
            return new InvalidOperationException($"Symbols are not loaded, so '{member}' cannot be answered. "
                                                 + "Open the code with CodeBase.Open(...).WithSymbols() to resolve symbols; "
                                                 + "the fast mode only knows the syntax.");
        }
    }
}
