using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using Argument.Check;
using Solution.Parser.Project;
using Solution.Parser.Sln;

namespace Solution.Parser.CSharp
{
    /// <summary>How much of the C# code <c>SolutionFile.Parse(path, mode)</c> makes available.</summary>
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
    /// without a mode gets the syntax only, on first use; <c>SolutionFile.Parse(path)</c> stays exactly
    /// what it always was.
    /// </remarks>
    /// <example>
    /// <code>
    /// var solution = SolutionFile.Parse(path, ParseMode.WithSymbols);
    ///
    /// from call in solution.UnitTestTrees.AllInvocations().Named("AssertPostAsync")
    /// where call.Passes("writeResponse", true)
    /// select $"{call.Location}: writeResponse is true";
    /// </code>
    /// </example>
    public static class SolutionCode
    {
        private static readonly ConditionalWeakTable<SolutionFile, CodeBase> CodeBases = new();

        private static readonly ConditionalWeakTable<ProjectFile, SolutionFile> Owners = new();

        private static readonly ConditionalWeakTable<ProjectFile, CodeBase> StandaloneProjects = new();

        extension(SolutionFile solution)
        {
            /// <summary>Parses the sln or slnx at <paramref name="path"/> with its C# code in the given mode.</summary>
            public static SolutionFile Parse(string path, ParseMode mode)
            {
                Throw.IfNullOrWhiteSpace(path);

                return SolutionFile.Parse(new FileInfo(path), mode);
            }

            /// <summary>Parses the given sln or slnx with its C# code in the given mode.</summary>
            public static SolutionFile Parse(FileInfo fileInfo, ParseMode mode)
            {
                var parsed = SolutionFile.Parse(fileInfo);
                var code = CodeBase.Open(parsed);

                Attach(parsed, mode == ParseMode.WithSymbols ? code.WithSymbols() : code);

                return parsed;
            }

            /// <summary>
            /// Finds the solution from <paramref name="startDirectory"/> upwards and parses it with its C#
            /// code in the given mode, see <see cref="SolutionFile.Find(string, DirectoryInfo?)"/>.
            /// </summary>
            public static SolutionFile Find(string solutionName, ParseMode mode, DirectoryInfo? startDirectory = null)
            {
                var found = SolutionFile.Find(solutionName, startDirectory);

                Attach(found, mode == ParseMode.WithSymbols ? CodeBase.Open(found).WithSymbols() : CodeBase.Open(found));

                return found;
            }

            /// <summary>True for a solution parsed with <see cref="ParseMode.WithSymbols"/>.</summary>
            public bool HasSymbols => CodeOf(solution).HasSymbols;

            /// <summary>Every syntax tree of every project, a file linked into two projects once.</summary>
            public ImmutableList<CSharpSyntaxTree> AllTrees => CodeOf(solution).AllTrees;

            public ImmutableList<CSharpSyntaxTree> ProductionTrees => CodeOf(solution).ProductionTrees;

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

            foreach (var project in solution.Projects.AddRange(solution.ProductionProjects).AddRange(solution.UnitTestProjects))
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
