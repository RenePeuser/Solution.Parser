using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Argument.Check;
using Solution.Parser.Project;
using Solution.Parser.Sln;

namespace Solution.Parser.CSharp
{
    /// <summary>How much of the C# code <see cref="SolutionCode.Parse(SolutionFileInfo, ParseMode)"/> makes available.</summary>
    public enum ParseMode
    {
        /// <summary>Syntax trees only, parsed once on first use and cached. A symbol access throws.</summary>
        SyntaxOnly,

        /// <summary>
        /// Syntax trees and symbols. Nothing is compiled yet; each project is compiled the first time one
        /// of its symbols is asked for.
        /// </summary>
        WithSymbols
    }

    /// <summary>
    /// The C# code of a solution, right on <see cref="SolutionFile"/> and <see cref="ProjectFile"/>:
    /// <c>solution.UnitTestTrees</c>, <c>project.Trees</c>, <c>solution.CompilationDiagnostics()</c>.
    /// </summary>
    /// <remarks>
    /// The code hangs on the <see cref="SolutionFile"/> instance, so the caches live as long as the
    /// solution does and two calls to <c>Parse</c> give two independent code bases. A solution parsed
    /// without a mode gets the syntax only, on first use; <c>solutionFileInfo.Parse()</c> stays exactly
    /// what it always was.
    /// </remarks>
    /// <example>
    /// <code>
    /// var solution = solutionFileInfo.Parse(ParseMode.WithSymbols);
    ///
    /// from call in solution.UnitTestTrees.AllInvocations().Named("AssertPostAsync")
    /// where call.Argument("writeResponse")?.Is(true) == true
    /// select $"{call.Location}: writeResponse is true";
    /// </code>
    /// </example>
    public static class SolutionCode
    {
        private static readonly ConditionalWeakTable<SolutionFile, CodeBase> CodeBases = new();

        private static readonly ConditionalWeakTable<ProjectFile, SolutionFile> Owners = new();

        private static readonly ConditionalWeakTable<ProjectFile, CodeBase> StandaloneProjects = new();

        public static SolutionFile Parse(this SolutionFileInfo solutionFileInfo, ParseMode mode)
        {
            Throw.IfNull(solutionFileInfo);

            var solution = solutionFileInfo.Parse();
            var code = CodeBase.Open(solution);

            Attach(solution, mode == ParseMode.WithSymbols ? code.WithSymbols() : code);

            return solution;
        }

        extension(SolutionFile solution)
        {
            /// <summary>True for a solution parsed with <see cref="ParseMode.WithSymbols"/>.</summary>
            public bool HasSymbols => CodeOf(solution).HasSymbols;

            /// <summary>Every syntax tree of every project, a file linked into two projects once.</summary>
            public ImmutableList<CSharpSyntaxTree> AllTrees => CodeOf(solution).AllTrees;

            public ImmutableList<CSharpSyntaxTree> ProductiveTrees => CodeOf(solution).ProductiveTrees;

            public ImmutableList<CSharpSyntaxTree> UnitTestTrees => CodeOf(solution).UnitTestTrees;

            /// <summary>What the compiler reports for every project; needs <see cref="ParseMode.WithSymbols"/>.</summary>
            public ImmutableList<ParseDiagnostic> CompilationDiagnostics()
            {
                return CodeOf(solution).CompilationDiagnostics();
            }
        }

        private static CodeBase CodeOf(SolutionFile solution)
        {
            Throw.IfNull(solution);

            if (!CodeBases.TryGetValue(solution, out var code))
            {
                // Parsed without a mode: the syntax only. A racing caller may attach first, its code wins.
                Attach(solution, CodeBase.Open(solution));
                CodeBases.TryGetValue(solution, out code);
            }

            return code!;
        }

        internal static CodeBase CodeOf(ProjectFile project)
        {
            Throw.IfNull(project);

            // A project whose solution has no code yet, or that was parsed on its own, gets the syntax of
            // its own files. Once the solution has code, the project answers out of that.
            return Owners.TryGetValue(project, out var solution)
                       ? CodeOf(solution)
                       : StandaloneProjects.GetValue(project, CodeBase.Open);
        }

        private static void Attach(SolutionFile solution, CodeBase code)
        {
            CodeBases.TryAdd(solution, code);

            foreach (var project in solution.Projects.AddRange(solution.ProductiveProjects).AddRange(solution.UnitTestProjects))
            {
                Owners.AddOrUpdate(project, solution);
            }
        }
    }

    /// <summary>
    /// The C# code of a project, out of the code of the solution it was parsed with. A project parsed
    /// on its own, or one of a solution that was not asked for its code yet, gets the syntax of its own
    /// files.
    /// </summary>
    public static class ProjectCode
    {
        extension(ProjectFile project)
        {
            /// <summary>The syntax trees of the project.</summary>
            public ImmutableList<CSharpSyntaxTree> Trees => SolutionCode.CodeOf(project).Trees(project);

            /// <summary>
            /// What the compiler reports for the project; needs <see cref="ParseMode.WithSymbols"/>. Errors
            /// here mean symbols may be missing, for example because the solution was not restored.
            /// </summary>
            public ImmutableList<ParseDiagnostic> CompilationDiagnostics()
            {
                return SolutionCode.CodeOf(project).CompilationDiagnostics(project);
            }
        }
    }
}
