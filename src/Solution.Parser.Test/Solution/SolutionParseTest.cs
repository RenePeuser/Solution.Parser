using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Argument.Check;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using Solution.Parser.Solution;

namespace Solution.Parser.Test.Solution
{
    [TestClass]
    public class SolutionParseTest
    {
        private static SolutionFileInfo _solutionFileInfo = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            var solutionFile = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
            Throw.IfNull(solutionFile);

            _solutionFileInfo = solutionFile;
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
        public void Assert_That_A_CSharp_File_Can_Be_Parsed()
        {
            var tcSolutionFile = _solutionFileInfo.Parse();

            var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.CSharpFileInfos).Select(c => c.Parse()).ToList();

            Assert.IsTrue(csharpSyntaxTrees.Any());
        }

        [TestMethod]
        public void File_Scoped_Namespaces_Should_Be_Parseable_Too()
        {
            var tcSolutionFile = _solutionFileInfo.Parse();

            var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.CSharpFileInfos).Select(c => c.Parse()).ToList();

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
