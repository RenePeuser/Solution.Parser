using System;
using System.IO;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Core;
using Solution.Parser.Sln;

namespace Solution.Parser.CSharp.Semantic.Test
{
    /// <summary>
    /// The short ways in: finding a solution, picking a project, asking what a call passes and failing a
    /// rule. Each of them replaces a chain a consumer used to write by hand.
    /// </summary>
    [TestClass]
    public class ConsumerApiTest
    {
        private const string ConsumerApiFix = "Check the shortcut, it has to answer what the long way answers";

        private const string Client = """
            using System.Net.Http;
            using System.Threading.Tasks;

            public static class ClientExtensions
            {
                public static Task AssertPostAsync(this HttpClient client, string url, string body, bool writeResponse = true) => Task.CompletedTask;
            }

            """;

        [TestMethod]
        public void Find_Locates_And_Parses_The_Solution_From_The_Running_Assembly()
        {
            var solution = SolutionFile.Find("Solution.Parser.sln");

            Assert.That.AreEqual("Solution.Parser.sln", solution.SolutionFileInfo.Name, because: "the search walks up from the test output into the repository", fix: ConsumerApiFix);
            Assert.That.IsFalse(solution.HasSymbols, because: "without a mode the syntax comes on first use, as with Parse()", fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Find_Without_Extension_Accepts_Either_Format()
        {
            var solution = SolutionFile.Find("Solution.Parser");

            Assert.That.AreEqual("Solution.Parser", solution.SolutionFileInfo.FileNameWithoutExtension, because: "a name without extension means sln or slnx", fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Find_With_A_Mode_Parses_The_Code_In_That_Mode()
        {
            var solution = SolutionFile.Find("Solution.Parser.sln", ParseMode.WithSymbols);

            Assert.That.IsTrue(solution.HasSymbols, because: "the mode is passed on as with Parse(mode)", fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Project_Picks_A_Project_By_Name()
        {
            var project = SolutionFile.Find("Solution.Parser.sln").Project("Solution.Parser.CSharp");

            Assert.That.AreEqual("Solution.Parser.CSharp", project.Name, because: "the name is the csproj file name without extension", fix: ConsumerApiFix);
            Assert.That.AreEqual(project.ProjectFileInfo.FullName, project.Path, because: "Path is the full path of the csproj", fix: ConsumerApiFix);
            Assert.That.IsTrue(project.Trees.Count > 0, because: "the project hands out its code right away", fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Project_With_An_Unknown_Name_Lists_The_Ones_There_Are()
        {
            var solution = SolutionFile.Find("Solution.Parser.sln");

            var exception = Assert.ThrowsExactly<ArgumentException>(() => solution.Project("Does.Not.Exist"));

            Assert.That.IsTrue(exception.Message.Contains("Solution.Parser.CSharp", StringComparison.Ordinal),
                               because: "a typo is fixed fastest when the message shows the right names",
                               fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Two_Infos_Of_The_Same_File_Are_Equal()
        {
            var solution = SolutionFile.Find("Solution.Parser.sln");

            var again = new SolutionFileInfo(solution.SolutionFileInfo.FullName);

            Assert.That.AreEqual(solution.SolutionFileInfo, again, because: "a file info stands for its file, not for the instance", fix: ConsumerApiFix);
            Assert.That.AreEqual(solution.SolutionFileInfo.FullName, again.ToString(), because: "ToString renders the path", fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Passes_Answers_By_Position_By_Name_And_By_Default()
        {
            var code = CodeBase.FromSource(Client + """
                public class Tests
                {
                    private readonly HttpClient Client = new();

                    public async Task Positional() => await Client.AssertPostAsync("url", "body", true);
                    public async Task Named() => await Client.AssertPostAsync("url", "body", writeResponse: false);
                    public async Task Default() => await Client.AssertPostAsync("url", "body");
                }
                """).WithSymbols();

            var calls = code.AllTrees.AllInvocations().Named("AssertPostAsync");

            Assert.That.IsTrue(calls[0].Passes("writeResponse", true), because: "the third argument fills writeResponse", fix: ConsumerApiFix);
            Assert.That.IsTrue(calls[1].Passes("writeResponse", false), because: "a named argument binds by name", fix: ConsumerApiFix);
            Assert.That.IsTrue(calls[2].Passes("writeResponse", true), because: "a left out argument passes the default", fix: ConsumerApiFix);
            Assert.That.IsFalse(calls[0].Passes("noSuchParameter", true), because: "a parameter the method lacks is passed nothing", fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Passes_Is_False_For_A_Call_That_Does_Not_Resolve()
        {
            var code = CodeBase.FromSource("""
                public class Tests
                {
                    public void Go() => Missing.Call(true);
                }
                """).WithSymbols();

            Assert.That.IsFalse(code.AllTrees.AllInvocations().Single().Passes("anything", true), because: "an unresolved call binds no argument", fix: ConsumerApiFix);
        }

        [TestMethod]
        public void Named_Filters_Any_Declaration_And_Attributes_With_Or_Without_Suffix()
        {
            var tree = CodeBase.FromSource("""
                using System;

                public class A
                {
                    [Obsolete] public void Dispose() { }
                    [ObsoleteAttribute("x")] public void Other() { }
                }
                """).AllTrees.Single();

            Assert.That.HasCount(1, tree.AllMethods().Named("Dispose"), because: "Named works on methods as on calls", fix: ConsumerApiFix);
            Assert.That.HasCount(2,
                                 tree.AllMethods().SelectMany(m => m.Attributes).Named("Obsolete"),
                                 because: "an attribute matches with and without its suffix, and Named gives all of them",
                                 fix: ConsumerApiFix);
        }

        [TestMethod]
        public void ShouldBeEmpty_Passes_Without_Findings()
        {
            Array.Empty<string>().ShouldBeEmpty("Nothing to find");
        }

        [TestMethod]
        public void ShouldBeEmpty_Lists_Every_Finding_On_Its_Own_Line()
        {
            string[] findings = [@"D:\repo\A.cs(1,1): A is mutable", @"D:\repo\B.cs(2,5): B is mutable"];

            var exception = Assert.ThrowsExactly<CodeRuleViolationException>(() => findings.ShouldBeEmpty("Properties must be immutable"));

            Assert.That.AreEqual("Properties must be immutable", exception.Rule, because: "the rule is the headline", fix: ConsumerApiFix);
            Assert.That.AreEqual<string>(findings, exception.Findings, because: "every finding is kept as rendered", fix: ConsumerApiFix);
            Assert.That.IsTrue(exception.Message.Contains($"2 findings:{Environment.NewLine}  {findings[0]}{Environment.NewLine}  {findings[1]}", StringComparison.Ordinal),
                               because: "one finding per line keeps the locations clickable",
                               fix: ConsumerApiFix);
        }
    }
}
