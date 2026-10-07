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
    /// The full mode against this library's own solution: package references from
    /// project.assets.json, project references between compilations, the framework reference packs.
    /// </summary>
    [TestClass]
    public class SolutionSemanticsTest
    {
        private const string CompilationFix = "Check ProjectInput.From and Workspace.Build, a reference the real build has is missing";

        private static CodeBase _code = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            var solutionFile = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));

            _code = CodeBase.Open(solutionFile).WithSymbols();
        }

        private static ProjectFile Project(string name)
        {
            return _code.Solution!.Projects.Single(p => p.ProjectFileInfo.FileNameWithoutExtenion == name);
        }

        [TestMethod]
        [DataRow("Solution.Parser.Core")]
        [DataRow("Solution.Parser.CSharp")]
        [DataRow("Solution.Parser.CSharp.Semantic")]
        [DataRow("Solution.Parser.Project")]
        [DataRow("Solution.Parser.Sln")]
        [DataRow("Solution.Parser.Xaml")]
        [DataRow("Solution.Parser.Nuspec")]
        [DataRow("Solution.Parser.AspNet")]
        [DataRow("Solution.Parser.CSharp.Test")]
        [DataRow("Solution.Parser.CSharp.Semantic.Test")]
        [DataRow("Solution.Parser.Sln.Test")]
        [DataRow("Solution.Parser.Xaml.Test")]
        [DataRow("Solution.Parser.Architecture.Test")]
        [DataRow("SampleApp.Wpf.Test")]
        [DataRow("SampleApp.Wpf")]
        public void A_Restored_Project_Compiles_Without_Errors(string projectName)
        {
            var errors = _code.CompilationDiagnostics(Project(projectName))
                              .Where(d => d.IsError)
                              .Select(e => $"{e.Location}: {e.Id} {e.Message}")
                              .ToList();

            Assert.That.HasCount(0,
                                 errors,
                                 because: "every error is a symbol the full mode cannot resolve:" + Environment.NewLine + string.Join(Environment.NewLine, errors),
                                 fix: CompilationFix);
        }

        [TestMethod]
        public void A_Call_Into_A_Package_Knows_The_Package_And_The_Parameter_Names()
        {
            var call = _code.Trees(Project("Solution.Parser.Architecture.Test"))
                            .AllInvocations()
                            .Named("HasCount")
                            .First();

            Assert.That.IsTrue(call.IsResolved, because: "AspNetCore.Simple.MsTest.Sdk is restored for the test project", fix: CompilationFix);
            Assert.That.AreEqual("AspNetCore.Simple.MsTest.Sdk", call.Method!.Origin.Name, because: "the assembly path maps back to its package", fix: "Check AssetsFile and Workspace.PackageOf");
            Assert.That.IsTrue(call.Method!.Origin.IsPackage, because: "it comes from a NuGet package", fix: "Check AssetsFile and Workspace.PackageOf");
            Assert.That.IsTrue(call.Argument("because")?.IsExplicit == true,
                               because: "the parameter name comes from the package metadata, not from the source",
                               fix: CompilationFix);
        }

        [TestMethod]
        public void A_Call_Into_A_Referenced_Project_Is_The_Declared_Method()
        {
            var call = _code.Trees(Project("Solution.Parser.CSharp.Test"))
                            .AllInvocations()
                            .Named("Parse")
                            .First(c => c.FilePath.EndsWith("ParseHelper.cs", StringComparison.OrdinalIgnoreCase));

            Assert.That.IsTrue(call.Method?.Origin.IsSource == true, because: "CSharpParser is part of the solution", fix: CompilationFix);
            Assert.That.IsTrue(call.Method!.FilePath.EndsWith("CSharpParser.cs", StringComparison.OrdinalIgnoreCase),
                               because: "a project reference resolves to the source of the other project, not to its dll",
                               fix: "Check that Workspace.Build references other projects as compilations");
        }

        [TestMethod]
        public void A_Package_Call_In_Productive_Code_Resolves_Too()
        {
            var calls = _code.Trees(Project("Solution.Parser.CSharp"))
                             .AllInvocations()
                             .Named("IfNull")
                             .Where(c => c.Target == "Throw")
                             .ToList();

            Assert.That.IsTrue(calls.Count > 0, because: "the parser guards its arguments with Throw.IfNull", fix: "Find another package call to test with");
            Assert.That.All(calls,
                            c => c.Method?.Origin.Name == "Argument.Check",
                            "resolves into Argument.Check",
                            because: "transitive packages are listed in project.assets.json as well",
                            fix: CompilationFix);
        }
    }
}
