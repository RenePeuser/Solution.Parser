# Solution Parser

## Solution formats

Both the classic `.sln` and the new xml based `.slnx` format are supported:

```csharp
// classic
var solutionFileInfo = new SolutionFileName("MySolution.sln").FindSolutionFileReverseFrom(startUpDirectory);

// slnx
var solutionFileInfo = new SolutionFileName("MySolution.slnx").FindSolutionFileReverseFrom(startUpDirectory);

// either of both, '.slnx' wins when both files exist side by side
var solutionFileInfo = SolutionFileName.WithAnySolutionFormat("MySolution").FindSolutionFileReverseFrom(startUpDirectory);
```

`SolutionFileInfo.IsSlnx` tells which format was found. Everything behind `Parse()` is format agnostic.

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



## What the model gives you

Every declaration carries a `Location`, so a finding points at the source instead of only naming the
type. `Location.ToString()` renders `path(line,column)`, which test runners and IDEs turn into a link:

```csharp
var findings = from tree in ProductiveCode
               from type in tree.AllTypes()
               from property in type.Properties
               where property.IsReadOnly.IsFalse()
               select $"{property.Location}: {type.FullQualifiedName}.{property.Name} is mutable";

// D:\repo\src\Person.cs(12,5): My.Sample.Person.Name is mutable
```

Beyond that, a declaration reports its `Accessibility` (including the default that applies when no
modifier is written), its `Documentation` (the XML comment), its `Modifiers` (now complete, including
`sealed`, `virtual`, `override`, `new`, `async` and `readonly`), its `TypeParameters` with their
constraints, and its `Attributes` split into positional and named arguments. `HasAttribute("Obsolete")`
matches `[Obsolete]`, `[ObsoleteAttribute]` and `[System.ObsoleteAttribute]` alike.

Types also report `Indexers`, `Operators`, `Delegates` and `Finalizers`, and `TypeKind` tells a
`record` from a `record struct`. `tree.Diagnostics` surfaces what Roslyn reported while parsing, so a
rule can assert that no file has a syntax error rather than silently trusting an incomplete model.

## Nesting

A member belongs to the type that declares it. `type.Methods` holds the methods of that type, and the
methods of a nested type belong to the nested type:

```csharp
tree.Types              // the types declared at file level
type.NestedTypes        // the types declared directly inside this type
type.Methods            // the methods declared directly in this type
```

To walk everything, ask for it:

```csharp
tree.AllTypes()               // every type of the file, nested ones included
tree.AllMethods()             // every method of the file
type.DescendantTypes()        // every type declared inside this one, at any depth
type.AllMethods()             // this type and every type nested in it
ProductiveCode.AllTypes()     // the same across a whole solution
```

Predicates that read the way a rule means: `IsPublic()`, `IsStatic()`, `IsSealed()`, `IsAbstract()`,
`IsPartial()`, `IsOverride()`, `IsVirtual()`, `HasAttribute(name)`, `Implements(name)`,
`InheritsFrom(name)`.

## Migrating from 5.x

| Before | Now |
|---|---|
| `type.Methods` returned nested types' methods too | `type.AllMethods()` |
| `tree.Classes` contained nested classes | `tree.AllTypes()`, filtered by kind |
| `type.NestedClasses` contained grandchildren | `type.DescendantTypes()` |
| `Record : Class : Interface` | all siblings below `TypeDeclaration`, with `Kind` |
| `class.Interfaces` (was a duplicate of `NestedInterfaces`) | `BaseTypes`, or `Implements(name)` |
| `method.SyntaxTree` returned the whole file | it is now the method's own source; the file is `tree.SyntaxTree` |
| `new Class(...)` positional | object initializer, `new Class { Name = ..., ... }` |

`Classes`, `Records`, `Interfaces`, `Structs` and `Enums` still exist on the tree, and
`NestedClasses`, `NestedStructs`, `NestedInterfaces` and `NestedEnums` still exist on a type; they are
now filtered views over `Types` and `NestedTypes`. `Method.MethodValue` and `Method.MethodBody` are
kept as the previous names for `SyntaxTree` and `Body`.
