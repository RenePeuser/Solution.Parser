using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using Solution.Parser.Sln;

[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]

namespace Solution.Parser.Sln.Test
{
    [TestClass]
    public class SolutionParseTest
    {
        private static SolutionFileInfo _solutionFileInfo = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            var solutionFile = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));

            _solutionFileInfo = solutionFile;
        }


        [Ignore]
        [TestMethod]
        public void Count_CSharp_Files()
        {
            var parsedSolutionFile = _solutionFileInfo.Parse();

            var csharpFiles = parsedSolutionFile.Projects.SelectMany(p => p.SourceFiles.GetCSharpFiles()).ToList();
            var projects = parsedSolutionFile.Projects.Count;
            var parsedCSharpFiles = csharpFiles.Select(c => c.Parse());
            var classes = parsedCSharpFiles.SelectMany(c => c.Classes);
            var maxLineOfSyntaxTree = classes.Max(c => c.SyntaxTree.Split(Environment.NewLine).Length);
            var maxMethodLineCount = classes.SelectMany(c => c.Methods).Max(m => m.MethodBody.Split(Environment.NewLine).Length);
            var enums = parsedCSharpFiles.SelectMany(c => c.Enums).Count();
            var interfaces = parsedCSharpFiles.SelectMany(c => c.Interfaces).Count();
            var records = parsedCSharpFiles.SelectMany(c => c.Records).Count();
            var structs = parsedCSharpFiles.SelectMany(c => c.Structs).Count();
            var totalLines = parsedCSharpFiles.Sum(c => c.SyntaxTree.Split(Environment.NewLine).Length);
            var testClasses = classes.Count(c => c.Attributes.Any(a => a.Name == "TestClass"));
            var testMethods = classes.SelectMany(c => c.Methods).Count(m => m.Attributes.Any(a => a.Name == "TestMethod"));

            var record = new
            {
                Projects = projects,
                CSharpFiles = csharpFiles.Count,
                Classes = classes.Count(),
                Records = records,
                Enums = enums,
                Interfaces = interfaces,
                Structs = structs,
                MaxLinesOfOneSyntaxTree = maxLineOfSyntaxTree,
                MaxLinesOfOneMethodBody = maxMethodLineCount,
                TotalLinesOfAllCSharpFiles = totalLines,
            };

            var tests = new
            {
                TestClassAttributes = testClasses,
                TestMethodAttributes = testMethods
            };

            Assert.Fail($"{Environment.NewLine}{Environment.NewLine}{_solutionFileInfo.FileNameWithoutExtenion}:{Environment.NewLine}{ConsoleTables.ConsoleTable.From([record])}{Environment.NewLine}{Environment.NewLine}{ConsoleTables.ConsoleTable.From([tests])}");

        }

        [TestMethod]
        public void Assert_That_User_Nuget_Folder_Was_Found()
        {
            var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nugetDirectory = new DirectoryInfo(Path.Combine(userPath, ".nuget"));

            Assert.IsTrue(nugetDirectory.Exists);
        }

        [TestMethod]
        public void Assert_That_Solution_Could_Be_Parsed()
        {
            var parsedSolutionFile = _solutionFileInfo.Parse();

            Assert.IsNotNull(parsedSolutionFile);
        }

        [TestMethod]
        public void Assert_That_Correct_Test_Project_Will_Be_Detected()
        {
            var parsedSolutionFile = _solutionFileInfo.Parse();

            var testProjects = parsedSolutionFile.UnitTestProjects
                                                 .Select(p => p.ProjectFileInfo.FileNameWithoutExtenion)
                                                 .ToImmutableList();

            // One test project per package since the split, so a fixed count would only record how
            // many there happen to be today.
            Assert.IsTrue(testProjects.Contains("Solution.Parser.Xaml.Test"), "the xaml tests are a test project");
            Assert.IsTrue(testProjects.Contains("Solution.Parser.CSharp.Test"), "the csharp tests are a test project");
            Assert.IsFalse(testProjects.Contains("Solution.Parser.Xaml"), "a library is not a test project");
        }

        [TestMethod]
        public void Assert_That_Correct_Productive_Project_Will_Be_Detected()
        {
            var parsedSolutionFile = _solutionFileInfo.Parse();

            var productiveProjects = parsedSolutionFile.ProductiveProjects
                                                       .Select(p => p.ProjectFileInfo.FileNameWithoutExtenion)
                                                       .ToImmutableList();

            // Asserting the exact count would break every time the solution gains a project, which
            // is what the split just did. What the rule is actually about: the library projects are
            // productive and the test project is not.
            Assert.IsTrue(productiveProjects.Contains("Solution.Parser"), "the meta project is productive");
            Assert.IsTrue(productiveProjects.Contains("Solution.Parser.Xaml"), "a split library project is productive");
            Assert.IsFalse(productiveProjects.Contains("Solution.Parser.Test"), "a test project is not productive");
        }

        [TestMethod]
        public void Assert_That_A_CSharp_File_Can_Be_Parsed()
        {
            var tcSolutionFile = _solutionFileInfo.Parse();

            var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.SourceFiles.GetCSharpFiles()).Select(c => c.Parse()).ToList();

            Assert.IsTrue(csharpSyntaxTrees.Any());
        }

        [TestMethod]
        public void File_Scoped_Namespaces_Should_Be_Parseable_Too()
        {
            var tcSolutionFile = _solutionFileInfo.Parse();

            var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.SourceFiles.GetCSharpFiles()).Select(c => c.Parse()).ToList();

            var csharpParser = csharpSyntaxTrees.SelectMany(csharp => csharp.Classes).Where(c => c.Name == "CSharpParser").ToImmutableList();

            Assert.AreEqual("Solution.Parser.CSharp.CSharpParser", csharpParser[0].FullQualifiedName);
        }

        [TestMethod]
        public void Should_Be_Able_To_Parse_Structs()
        {
            const string AsyncMethods_Should_Detect_In_Struct = @"""
namespace RunJIT.CodeRules.Internal.ExampleCode.WebApi.Performance
{
    public struct AsyncMethods_Should_Detect
    {
        public Task Should_Detect_Method_With_Only_Task()
        {
            return Task.CompletedTask;
        }
    }
}
""";

            var csharpSyntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(AsyncMethods_Should_Detect_In_Struct);
            var syntaxTree = CSharpParser.Parse(csharpSyntaxTree, string.Empty);

            Assert.IsTrue(syntaxTree.Structs.Any());
        }
    }
}
