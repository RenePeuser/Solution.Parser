using System;
using System.IO;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Project;
using Solution.Parser.Sln;

namespace Solution.Parser.CSharp.Semantic.Test
{
    /// <summary>
    /// The code of a solution right on <see cref="SolutionFile"/> and its projects, without a
    /// <see cref="CodeBase"/> in sight.
    /// </summary>
    [TestClass]
    public class SolutionCodeTest
    {
        private const string SolutionCodeFix = "Check SolutionCode, the code hangs on the SolutionFile instance";

        // These tests are about Parse(path, mode), so they start from the path rather than from Find.
        private static readonly string SolutionPath = SolutionFile.Find("Solution.Parser.sln").Path;

        [TestMethod]
        public void A_Solution_Parsed_Without_A_Mode_Gets_The_Syntax_On_First_Use()
        {
            var solution = SolutionFile.Parse(SolutionPath);

            Assert.That.IsTrue(solution.ProductionTrees.Count > 0, because: "the syntax costs nothing until it is asked for", fix: SolutionCodeFix);
            Assert.That.IsFalse(solution.HasSymbols, because: "symbols are only there when asked for at parse time", fix: SolutionCodeFix);
        }

        [TestMethod]
        public void The_Mode_Decides_Whether_There_Are_Symbols()
        {
            Assert.That.IsFalse(SolutionFile.Parse(SolutionPath, ParseMode.SyntaxOnly).HasSymbols, because: "SyntaxOnly is the fast mode", fix: SolutionCodeFix);
            Assert.That.IsTrue(SolutionFile.Parse(SolutionPath, ParseMode.WithSymbols).HasSymbols, because: "WithSymbols is the full mode", fix: SolutionCodeFix);
        }

        [TestMethod]
        public void A_Project_Has_Its_Own_Trees_Out_Of_The_Solution_Cache()
        {
            var solution = SolutionFile.Parse(SolutionPath, ParseMode.SyntaxOnly);
            var project = solution.Projects.Single(p => p.Name == "Solution.Parser.CSharp");

            var trees = project.Trees;

            Assert.That.IsTrue(trees.Count > 0, because: "the C# parser has source files", fix: SolutionCodeFix);
            Assert.That.All(trees,
                            t => solution.AllTrees.Any(s => ReferenceEquals(s, t)),
                            "is the very tree the solution holds",
                            because: "project and solution share one cache, a file is parsed once",
                            fix: SolutionCodeFix);
        }

        [TestMethod]
        public void A_Project_Of_A_Solution_Parsed_Without_A_Mode_Has_Its_Trees()
        {
            var solution = SolutionFile.Parse(SolutionPath);
            var project = solution.Projects.Single(p => p.Name == "Solution.Parser.CSharp");

            Assert.That.IsTrue(project.Trees.Count > 0, because: "Parse() stays compatible and the code comes on first use, also per project", fix: SolutionCodeFix);
        }

        [TestMethod]
        public void A_Project_Parsed_On_Its_Own_Has_Its_Trees()
        {
            var projectPath = SolutionFile.Parse(SolutionPath).Project("Solution.Parser.CSharp").Path;

            var project = ProjectFile.Parse(projectPath);

            Assert.That.IsTrue(project.Trees.Any(t => t.FileName.EndsWith("CSharpParser.cs", StringComparison.OrdinalIgnoreCase)),
                               because: "a project needs no solution to know its own files",
                               fix: SolutionCodeFix);
        }

        [TestMethod]
        public void Compilation_Diagnostics_Need_Symbols()
        {
            var solution = SolutionFile.Parse(SolutionPath, ParseMode.SyntaxOnly);

            var exception = Assert.ThrowsExactly<InvalidOperationException>(() => solution.CompilationDiagnostics());

            Assert.That.IsTrue(exception.Message.Contains("ParseMode.WithSymbols", StringComparison.Ordinal),
                               because: "the message must tell how to get symbols",
                               fix: "Check SymbolsNotLoaded.Exception");
        }
    }
}
