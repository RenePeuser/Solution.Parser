using System;
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
        private static SolutionFileInfo sSolutionFileInfo;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            sSolutionFileInfo =
                new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(
                    new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
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
            var tcSolutionFile = sSolutionFileInfo.Parse();

            Assert.IsNotNull(tcSolutionFile);
        }

        [TestMethod]
        public void Assert_That_A_CSharp_File_Can_Be_Parsed()
        {
            var tcSolutionFile = sSolutionFileInfo.Parse();

            var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.CSharpFileInfos).Select(c => c.Parse()).ToList();

            Assert.IsTrue(csharpSyntaxTrees.Any());
        }
    }
}
