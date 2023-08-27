# Solution Parser

## Sample
```csharp
[TestClass]
public class SolutionParseTest
{
    private static SolutionFileInfo _sSolutionFileInfo = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        var solutionFile = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
        _sSolutionFileInfo = Throw.IfNull(solutionFile);
    }

    [TestMethod]
    public void Assert_That_Solution_Could_Be_Parsed()
    {
        var tcSolutionFile = _sSolutionFileInfo.Parse();

        Assert.IsNotNull(tcSolutionFile);
    }

    [TestMethod]
    public void Assert_That_A_CSharp_File_Can_Be_Parsed()
    {
        var tcSolutionFile = _sSolutionFileInfo.Parse();

        var csharpSyntaxTrees = tcSolutionFile.Projects.SelectMany(p => p.CSharpFileInfos).Select(c => c.Parse()).ToList();

        Assert.IsTrue(csharpSyntaxTrees.Any());
    }
}
```
