# Solution Parser

## Sample
```csharp
 [TestClass]
 public abstract class MsTestBase
 {
     protected static IImmutableList<CSharpSyntaxTree> TestCode { get; private set; } = ImmutableList<CSharpSyntaxTree>.Empty;

     protected static IImmutableList<CSharpSyntaxTree> AllSyntaxTrees { get; private set; } = ImmutableList<CSharpSyntaxTree>.Empty;

     protected static IImmutableList<CSharpSyntaxTree> ProductiveCode { get; private set; } = ImmutableList<CSharpSyntaxTree>.Empty;

     protected static IImmutableList<CSharpSyntaxTree> ProductiveCodeToAnaylze { get; private set; } = ImmutableList<CSharpSyntaxTree>.Empty;

     protected static SolutionFile Solution { get; private set; } = null!;
      
     [AssemblyInitialize]
     public static void Init(TestContext _)
     {
         var sSolutionFileInfo = new SolutionFileName("MySolution.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
         Throw.IfNull(sSolutionFileInfo);

         Solution = sSolutionFileInfo.Parse();

         ProductiveCode = Solution.ProductiveProjects.SelectMany(p => p.CSharpFileInfos)                                 
												     .Select(c => c.Parse())
												     .ToImmutableList();

         TestCode = Solution.UnitTestProjects.SelectMany(p => p.CSharpFileInfos)
											 .Select(c => c.Parse())
											 .ToImmutableList();

         AllSyntaxTrees = ProductiveCode.Concat(TestCode).ToImmutableList();        
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


