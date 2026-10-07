# Solution.Parser

[![Build](https://github.com/RenePeuser/Solution.Parser/actions/workflows/build.yml/badge.svg)](https://github.com/RenePeuser/Solution.Parser/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Solution.Parser.svg)](https://www.nuget.org/packages/Solution.Parser/)
[![Downloads](https://img.shields.io/nuget/dt/Solution.Parser.svg)](https://www.nuget.org/packages/Solution.Parser/)
[![Build](https://github.com/RenePeuser/Solution.Parser/actions/workflows/build.yml/badge.svg)](https://github.com/RenePeuser/Solution.Parser/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Solution.Parser.svg)](https://www.nuget.org/packages/Solution.Parser/)
[![Downloads](https://img.shields.io/nuget/dt/Solution.Parser.svg)](https://www.nuget.org/packages/Solution.Parser/)
## Packages

One package ships: `Solution.Parser`. It contains everything, so an existing reference needs no
change. The split below is how the source is organised; the assemblies all travel inside that one
package and none of them is published on its own.

| Assembly | What it parses | What it drags in |
|---|---|---|
| `Solution.Parser.Core` | shared contracts, nothing on its own | â€” |
| `Solution.Parser.CSharp` | C# files into syntax trees | Roslyn |
| `Solution.Parser.CSharp.Semantic` | symbols: which method a call binds to, which parameter an argument fills | Roslyn, CSharp and Sln |
| `Solution.Parser.Xaml` | XAML markup | nothing but Core |
| `Solution.Parser.Project` | csproj, old and SDK style | nothing but Core |
| `Solution.Parser.Sln` | sln and slnx, and their projects | MSBuild |
| `Solution.Parser.Nuspec` | nuspec files and NuGet folders | nothing but Core |
| `Solution.Parser.AspNet` | controllers, routes, response types | NuGet client libraries |
| `Solution.Parser` | the shipped package, contains all of the above | all of the above |

`Solution.Parser.Project` deliberately knows no language: `ProjectFile.SourceFiles` is a plain file
list, and the typed views come from the language assembly you use.

```csharp
project.SourceFiles.GetCSharpFiles()   // Solution.Parser.CSharp
project.SourceFiles.GetXamlFiles()     // Solution.Parser.Xaml
```

Should the parts ever be published separately, that boundary is what lets a C# only consumer skip
XAML and the other way round. Today it is a source boundary, not a packaging one.

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

Every member with a body reports the method calls in it, lambdas included, a local function on its own:

```csharp
method.Invocations                         // Client.AssertPostAsync("..", "..", true)
tree.AllInvocations().Named("AssertPostAsync")
call.Target                                // "Client"
call.Arguments                             // as written, in source order
call.NamedArgument("writeResponse")        // only for writeResponse: true, see below for the rest
```

## Symbols (full mode)

The syntax alone cannot tell that `true` in `Client.AssertPostAsync("..", "..", true)` is the
parameter `writeResponse`, least of all when the method comes from a NuGet package. The compiler can.
The parse mode of the solution decides which of the two you get, over the same records:

```csharp
var solution = solutionFileInfo.Parse(ParseMode.SyntaxOnly);   // fast mode: syntax only, parsed once and cached
var solution = solutionFileInfo.Parse(ParseMode.WithSymbols);  // full mode: a Roslyn compilation per project, built on first use

var findings = from call in solution.UnitTestTrees.AllInvocations().Named("AssertPostAsync")
               where call.Argument("writeResponse")?.Is(true) == true
               select $"{call.Location}: writeResponse is true";
```

The code hangs on the solution and its projects: `solution.AllTrees`, `solution.ProductiveTrees`,
`solution.UnitTestTrees`, `project.Trees`, `solution.CompilationDiagnostics()` and
`project.CompilationDiagnostics()`. A solution parsed without a mode gets the syntax on first use.

What a resolved call tells, in the vocabulary of the syntax model:

```csharp
call.Resolution            // Resolved | Ambiguous | Unresolved
call.Method                // a Method record; for code of the solution the very record the tree holds
call.Method.Origin         // Source | Package("AspNetCore.Simple.MsTest.Sdk", "9.5.15") | Framework
call.BoundArguments        // every parameter with what is passed for it, in parameter order
call.Argument("writeResponse").Expression    // "true", by position or by name alike
call.Argument("writeResponse").IsExplicit    // false when left out and the default applies
call.Argument("writeResponse").Is(true)      // literal, const or default value alike
call.Candidates            // the competing overloads of an ambiguous call
call.Roslyn                // node, semantic model, symbol and operation, for anything not covered
```

The compilation is built from what restore and build left on disk: package assemblies from
`obj/project.assets.json`, the framework reference packs, project references as compilations, the
generated global usings and XAML output from obj, and the source generators of frameworks and packages.
The solution therefore has to be restored, which it is whenever its tests run. `project.CompilationDiagnostics()`
shows what the compiler could not resolve.

In the fast mode a symbol access throws instead of guessing, so the cost of a rule stays visible:
nothing is compiled unless a rule asks for symbols. `CodeBase.FromSources(("Sample.cs", code))` gives
the same API for code in memory, which is handy in tests.

## XAML

`.xaml` files parse into the same shape of model, so a rule can reach across markup and code behind:

```csharp
var tree = new XAMLFileInfo(@"D:\repo\src\MainWindow.xaml").Parse();

tree.FullQualifiedName          // "My.Sample.MainWindow", the x:Class
tree.Root                       // a Window, UserControl, Page, Application,
                                // ResourceDictionaryRoot or ControlRoot
tree.Diagnostics                // what could not be read, instead of a lost file
```

For markup that is not on disk, for example in a test:

```csharp
var tree = xamlContent.ParseXaml(@"D:\repo\src\MainWindow.xaml");
```

`Children` holds what an element declares itself, and a property element such as
`<Grid.RowDefinitions>` is a property of the Grid rather than a child of it, which is what XAML means
by it:

```csharp
element.Children                // the elements written directly inside this one
element.Properties              // attributes, attached properties and property elements
element.Resources               // what <X.Resources> declares
element["Grid.Row"]             // an attached property, by the name it is written with
element.Content                 // the text of <Button>Click me</Button>
element.Parent                  // the element this one is written in
element.Location                // path(line,column), clickable in a test runner
```

To walk everything, ask for it, the same way as on the C# side:

```csharp
tree.AllElements()              // every element of the file
tree.AllStyles()                // every Style, however deep in the resource dictionaries
tree.AllTemplates()             // DataTemplate, ControlTemplate, ItemsPanelTemplate, â€¦
tree.AllBindings()              // every Binding, nested ones included
tree.AllMarkupExtensions()      // every {â€¦}, nested ones included
tree.FindByName("Save")         // by x:Name
tree.FindByKey("OkButton")      // by x:Key
tree.OfTypeName("Button")       // every element written as <Button>
element.Ancestors()             // up the tree, nearest first
```

Values keep the shape they were written in, so a rule reads the part it cares about rather than the
raw string:

```csharp
var binding = textBlock["Text"]?.PropertyValue as Binding;

binding.Path?.ValueText                          // "Total"
(binding.Converter as StaticResource)?.ResourceKey   // "MoneyConverter"
binding.RelativeSource?.AncestorType             // "Window"
binding.StringFormat?.ValueText                  // "{0:#,##0.00} EUR"
```

`Binding`, `MultiBinding`, `StaticResource`, `DynamicResource`, `TemplateBinding`, `RelativeSource`,
`XTypeMarkupExtension`, `XStaticMarkupExtension` and `NullExtension` are all `MarkupExtension`, and an
extension the parser has no model for keeps its name and arguments instead of being reduced to text.
A value is markup only when it starts with an unescaped `{` followed by a name, so a pack URI, a
caption with a colon and a `{}{0:N2}` escape are all plain text.

`Style`, `Setter`, `Trigger`, `Template`, `ResourceDictionaryElement` and `Control` are siblings below
`ElementBase` with an `ElementKind`; `Window`, `UserControl`, `Page`, `Application`,
`ResourceDictionaryRoot` and `ControlRoot` are siblings below `Root` with a `RootKind`.

## Sample application

`src/SampleApp.Wpf` is a small but real WPF application: a window, a user control, a theme
dictionary, view models and bindings. It is a fixture rather than a demo. Because it is a real
`UseWPF` project, markup that WPF would reject cannot get in, so the parser tests always run against
XAML that actually compiles. Nothing in the library references it.

`src/SampleApp.Wpf.Test` is what a consumer of the packages looks like: it references
`Solution.Parser.Xaml`, `.CSharp` and `.Sln`, finds the application through the solution, and runs
code rules over it. Two of them are worth reading as examples:

- **BindingRule** - every `{Binding Path=X}` has to name a property that exists on the view model the
  view declares through `d:DataContext`. A typo there compiles, renders nothing and is caught by no
  compiler.
- **ResourceRule** - every `{StaticResource Key}` used anywhere has to be declared somewhere, across
  files.

Each rule is also run against a deliberately broken view parsed from memory, because a rule that
never fails proves nothing.

## Migrating

Upgrading from an older major? See **[MIGRATION.md](MIGRATION.md)** â€” it covers the mechanical
renames and, more importantly, the changes that still compile but report something different. It is
written so you can hand it to an AI assistant along with your rule suite.

The short version:

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

### Packaging and projects

| Before | Now |
|---|---|
| one `Solution.Parser` package | unchanged: still one package, now holding seven assemblies |
| `project.CSharpFileInfos` | `project.SourceFiles.GetCSharpFiles()` |
| `project.XAMLFileInfos` | `project.SourceFiles.GetXamlFiles()` |
| namespace `Solution.Parser.Common` | `Solution.Parser.Core` |
| namespace `Solution.Parser.Solution` | `Solution.Parser.Sln` |
| namespace `Solution.Parser.XAML` | `Solution.Parser.Xaml` |
| `XAMLFileInfo` | `XamlFileInfo` |

### XAML

| Before | Now |
|---|---|
| `element.Controls` held every descendant, each duplicated | `element.Children`, or `tree.AllElements()` |
| `element.Parent` was always null | it points at the element this one is written in |
| `element.Styles` was always empty | `tree.AllStyles()`, and `<Style>` is now a `Style` |
| `element.DataTemplates` | `tree.AllTemplates()` / `tree.AllDataTemplates()` |
| `element.DataContext` was set on every element | null unless the element sets one |
| `element.LineNumber` | `element.Location`, with line, column and path |
| `<Grid.RowDefinitions>` was a control named `Grid.RowDefinitions` | a `PropertyElement` in `Properties` |
| attached property named `RowProperty` | `Name` is `Row`, `FullQualifiedName` is `Grid.Row`, `DependencyPropertyName` is `RowProperty` |
| `x:Name` and `Name` were both called `Name` | `Property.Prefix` and `Property.IsXamlDirective` tell them apart |
| `Window : UserControl`, `DynamicResource : StaticResource` | siblings, with `RootKind` and a shared `ResourceReference` |
| `DataTemplate`, `ResourceDictionary`, `ResourceDictionaryControl` | `Template`, `ResourceDictionaryRoot`, `ResourceDictionaryElement` |
| `new Control(...)` positional | object initializer, `new Control { TypeName = ..., ... }` |
| an unreadable value threw and lost the file | `tree.Diagnostics` |

## License

Copyright 2022-2026 (c) Rene Peuser.

Licensed under the [Apache License 2.0](License.txt).
