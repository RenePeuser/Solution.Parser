using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using Solution.Parser.Solution;

namespace Solution.Parser.Test.Solution
{
    [TestClass]
    public class SolutionParseTest
    {
        private static readonly SolutionFileInfo SolutionFileInfo = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            var solutionFile = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
            Assert.IsNotNull(solutionFile, "Solution file could not be found");
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
            var parsedSolutionFile = SolutionFileInfo.Parse();

            Assert.IsNotNull(parsedSolutionFile);
        }

        [TestMethod]
        public void Assert_That_A_CSharp_File_Can_Be_Parsed()
        {
            var tcSolutionFile = SolutionFileInfo.Parse();

            var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.CSharpFileInfos).Select(c => c.Parse()).ToList();

            Assert.IsTrue(csharpSyntaxTrees.Any());
        }

        [TestMethod]
        public void File_Scoped_Namespaces_Should_Be_Parseable_Too()
        {
            var tcSolutionFile = SolutionFileInfo.Parse();

            var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.CSharpFileInfos).Select(c => c.Parse()).ToList();

            var csharpParser = csharpSyntaxTrees.SelectMany(csharp => csharp.Classes).Where(c => c.Name == "CSharpParser").ToImmutableList();

            Assert.AreEqual("Solution.Parser.CSharp.CSharpParser", csharpParser[0].FullQualifiedName);
        }
    }
}
