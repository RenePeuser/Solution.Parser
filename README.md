# Solution Parser

## Sample
```csharp
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
```

## CodeRule sample
```csharp
[TestCategory("Coding-Rules")]
[TestCategory("Coding-Rules Records")]
[TestClass]
public class Records : MsTestBase // <-- MsTestBase is a base class that provides the ProductiveCode property
{
    [TestMethod]
    public void Record_Properties_Have_To_Be_Be_Immutable()
    {
        var mutableProperties = (from syntaxTree in ProductiveCode
                                 from @record in syntaxTree.Records
                                 from property in @record.Properties
                                 where property.IsReadOnly.IsFalse()
                                 select new
                                 {
                                     Error = $@"

Please do not use mutable properties
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
FullName:  {@record.FullQualifiedName}
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Property:  {property.Type} {property.Name}
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Should be: {property.Type} {{get; init;}}
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
"
                                    }).ToImmutableList();

        Assert.IsTrue(mutableProperties.IsEmpty(),
            $"Properties must be immutable. Findings: '{mutableProperties.Count}':{Environment.NewLine}{mutableProperties.Select(error => error.Error).Flatten(Environment.NewLine)}{Environment.NewLine}");
        }
```


