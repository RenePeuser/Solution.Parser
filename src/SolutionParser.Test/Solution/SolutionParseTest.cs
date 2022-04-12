using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolutionParser.Solution;

namespace SolutionParser.Test.Solution
{
    [TestClass]
    public class SolutionParseTest
    {
        private static SolutionFileInfo sSolutionFileInfo;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            sSolutionFileInfo =
                new SolutionFileName("SolutionParser.sln").FindSolutionFileReverseFrom(
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
    }
}
