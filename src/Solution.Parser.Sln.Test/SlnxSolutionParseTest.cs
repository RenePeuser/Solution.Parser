using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Sln;

namespace Solution.Parser.Sln.Test
{
    [TestClass]
    public class SlnxSolutionParseTest
    {
        private static DirectoryInfo _repositoryRoot = null!;

        private static SolutionFileInfo _slnxFileInfo = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            var slnFileInfo = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));

            _repositoryRoot = slnFileInfo.Value.Directory!;
            _slnxFileInfo = new SolutionFileInfo(Path.Combine(_repositoryRoot.FullName, "src", "Solution.Parser.Sln.Test", "TestData", "Solution.Parser.slnx"));
        }

        [TestMethod]
        public void Assert_That_Slnx_File_Info_Is_Marked_As_Slnx()
        {
            Assert.IsTrue(_slnxFileInfo.IsSlnx);
            Assert.AreEqual("Solution.Parser", _slnxFileInfo.FileNameWithoutExtenion);
        }

        [TestMethod]
        public void Assert_That_Slnx_Solution_Could_Be_Parsed()
        {
            var parsedSolutionFile = _slnxFileInfo.Parse();

            Assert.IsNotNull(parsedSolutionFile);
            Assert.HasCount(2, parsedSolutionFile.Projects);
        }

        [TestMethod]
        public void Assert_That_Correct_Test_Project_Will_Be_Detected_In_Slnx()
        {
            var parsedSolutionFile = _slnxFileInfo.Parse();

            Assert.HasCount(1, parsedSolutionFile.UnitTestProjects);
            Assert.AreEqual("Solution.Parser.Xaml.Test", parsedSolutionFile.UnitTestProjects[0].ProjectFileInfo.FileNameWithoutExtenion);
        }

        [TestMethod]
        public void Assert_That_Correct_Productive_Project_Will_Be_Detected_In_Slnx()
        {
            var parsedSolutionFile = _slnxFileInfo.Parse();

            Assert.HasCount(1, parsedSolutionFile.ProductiveProjects);
            Assert.AreEqual("Solution.Parser.Xaml", parsedSolutionFile.ProductiveProjects[0].ProjectFileInfo.FileNameWithoutExtenion);
        }

        [TestMethod]
        public void Assert_That_Build_Dependencies_Of_Slnx_Will_Be_Resolved()
        {
            var parsedSolutionFile = _slnxFileInfo.Parse();

            var unitTestProject = parsedSolutionFile.UnitTestProjects[0];

            Assert.HasCount(1, unitTestProject.BuildDependencies);
            Assert.AreEqual("Solution.Parser.Xaml", unitTestProject.BuildDependencies[0].ProjectFileInfo.FileNameWithoutExtenion);
        }

        [TestMethod]
        [DataRow("MySolution.sln")]
        [DataRow("MySolution.slnx")]
        public void Assert_That_Solution_File_Name_Accepts_Both_Solution_Formats(string solutionFileName)
        {
            var result = new SolutionFileName(solutionFileName);

            Assert.IsTrue(result.HasExplicitFormat);
            Assert.AreEqual(solutionFileName, result.Value);
        }

        [TestMethod]
        public void Assert_That_Solution_File_Name_Rejects_A_Missing_Solution_Extension()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new SolutionFileName("MySolution.csproj"));
        }

        [TestMethod]
        public void Assert_That_Solution_File_Name_With_Any_Solution_Format_Keeps_An_Explicit_Format()
        {
            var result = SolutionFileName.WithAnySolutionFormat("MySolution.slnx");

            Assert.IsTrue(result.HasExplicitFormat);
        }

        [TestMethod]
        public void Assert_That_A_Solution_File_Will_Be_Found_When_No_Extension_Was_Given()
        {
            var result = SolutionFileName.WithAnySolutionFormat("Solution.Parser").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));

            Assert.AreEqual(Path.Combine(_repositoryRoot.FullName, "Solution.Parser.sln"), result.Value.FullName);
        }

        [TestMethod]
        public void Assert_That_Slnx_Wins_When_Both_Solution_Formats_Exist_Side_By_Side()
        {
            var temporaryDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"Solution.Parser.Test.{Guid.NewGuid():N}"));
            temporaryDirectory.Create();

            try
            {
                File.WriteAllText(Path.Combine(temporaryDirectory.FullName, "SideBySide.sln"), string.Empty);
                File.WriteAllText(Path.Combine(temporaryDirectory.FullName, "SideBySide.slnx"), string.Empty);

                var result = SolutionFileName.WithAnySolutionFormat("SideBySide").FindSolutionFileReverseFrom(temporaryDirectory);

                Assert.IsTrue(result.IsSlnx);
            }
            finally
            {
                temporaryDirectory.Delete(true);
            }
        }
    }
}
