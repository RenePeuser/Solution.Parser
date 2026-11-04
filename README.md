# Solution.Parser

[![Build](https://github.com/RenePeuser/Solution.Parser/actions/workflows/build.yml/badge.svg)](https://github.com/RenePeuser/Solution.Parser/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Solution.Parser.svg)](https://www.nuget.org/packages/Solution.Parser/)
[![Downloads](https://img.shields.io/nuget/dt/Solution.Parser.svg)](https://www.nuget.org/packages/Solution.Parser/)

**Architecture and code rules as plain unit tests.** Solution.Parser reads a whole solution, its
projects, C# code and XAML, into a typed model that is made to be queried with LINQ: statements,
expressions and patterns as trees, types taken apart, every finding with a clickable location. When
the syntax is not enough, the same records carry what the compiler knows: which method a call binds
to, which parameter an argument fills, which package a method comes from.

No Roslyn ceremony, no visitors, no `SyntaxKind`: a rule reads the way you would say it, which makes
it just as easy to write for a developer as for an AI agent. And every rule in the
[cookbook](#cookbook) below runs as a test in this repository, so what you copy works.

## Quick start

A code rule is a query over the solution and an assertion that it finds nothing:

```csharp
using Solution.Parser.Core;     // ShouldBeEmpty
using Solution.Parser.CSharp;   // the C# model and its queries
using Solution.Parser.Sln;      // SolutionFile

var solution = SolutionFile.Find("MySolution.sln");   // searched upwards from the running test

var findings = from property in solution.ProductionTrees.Properties
               where !property.IsReadOnly
               select $"{property.Location}: {property.FullyQualifiedName} is mutable";

findings.ShouldBeEmpty("Properties must be immutable");
```

A failing rule lists every finding on its own line, as `path(line,column): message`, which test
runners and IDEs turn into a link:

```
Properties must be immutable. 2 findings:
  D:\repo\src\Person.cs(12,5): My.Sample.Person.Name is mutable
  D:\repo\src\Order.cs(8,5): My.Sample.Order.Total is mutable
```

## Packages

One package ships: `Solution.Parser`. It contains everything, so an existing reference needs no
change. The split below is how the source is organised; the assemblies all travel inside that one
package and none of them is published on its own.

| Assembly | What it parses | What it drags in |
|---|---|---|
| `Solution.Parser.Core` | shared contracts, nothing on its own | — |
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
SolutionFile.Find("MySolution.sln")    // exactly that file
SolutionFile.Find("MySolution.slnx")   // exactly that file
SolutionFile.Find("MySolution")        // either of both, '.slnx' wins when both exist side by side

SolutionFile.Find("MySolution.sln", startDirectory: new DirectoryInfo(@"D:\repo"))   // search from elsewhere
```

`solution.FileInfo.Extension` tells which format was found. Everything behind it is format agnostic.
A solution whose path is already known is parsed with `SolutionFile.Parse(path)`.

## Solution, projects and code

```csharp
solution.Projects                       // every project
solution.ProductionProjects             // the ones that are not test projects
solution.UnitTestProjects
solution.Project("My.Sample")           // by name; an unknown name lists the ones there are

solution.AllTrees                       // the C# code of every project, parsed once and cached
solution.ProductionTrees
solution.UnitTestTrees
project.Trees

project.Name                            // "My.Sample"
project.Path                            // D:\repo\src\My.Sample\My.Sample.csproj
project.PackageReferences               // Include and Version
```

A file is a plain `FileInfo`: `solution.FileInfo`, `project.FileInfo`, `project.SourceFiles`. What kind
of file it is shows in the type parsing it returns, so each of them parses with a static `Parse` that
takes a path or a `FileInfo`: `SolutionFile.Parse`, `ProjectFile.Parse`, `CSharpSyntaxTree.Parse`,
`XamlSyntaxTree.Parse`, `NuspecFile.Parse`.

## A base class for a rule suite

```csharp
[TestClass]
public abstract class CodeRuleBase
{
    protected static SolutionFile Solution { get; private set; } = null!;

    [AssemblyInitialize]
    public static void Init(TestContext _) => Solution = SolutionFile.Find("MySolution.sln");
}

[TestClass]
public class Records : CodeRuleBase
{
    [TestMethod]
    public void Record_Properties_Have_To_Be_Immutable()
    {
        var findings = from tree in Solution.ProductionTrees
                       from record in tree.Records
                       from property in record.Properties
                       where !property.IsReadOnly
                       select $"{property.Location}: {record.FullyQualifiedName}.{property.Name} should be {{ get; init; }}";

        findings.ShouldBeEmpty("Record properties must be immutable");
    }
}
```

## Cookbook

Ready to copy. `trees` is `solution.ProductionTrees`, and each query ends in
`findings.ShouldBeEmpty("...")`. Every rule here runs word for word against broken and correct code
in `ReadmeSamplesTest` and `ReadmeSemanticSamplesTest`, so the samples cannot drift from the API.

### Time and async

```csharp
// Inject the clock: DateTime.Now cannot be tested
from member in trees.MembersWithBody
from access in member.MemberAccesses
where access.Target == "DateTime" && access.Name is "Now" or "UtcNow" or "Today"
select $"{access.Location}: inject TimeProvider instead of {access.SourceCode}";

// No async void, event handlers aside
from method in trees.Methods
where method.IsAsync && method.ReturnType.IsVoid
where !method.Parameters.LastOrDefault()?.TypeReference.Name.EndsWith("EventArgs", StringComparison.Ordinal) ?? true
select $"{method.Location}: {method.Name} is async void, return a Task";

// Awaitable methods end in Async
from method in trees.Methods
where method.ReturnType.IsTask && !method.Name.EndsWith("Async", StringComparison.Ordinal)
select $"{method.Location}: {method.Name} returns {method.ReturnType.Name}, call it {method.Name}Async";
```

### Logging and HTTP

```csharp
// Structured logging: a template, not an interpolated string
from call in trees.Invocations
where call.Name.StartsWith("Log", StringComparison.Ordinal) && call.Arguments.Any(a => a.Kind == ExpressionKind.InterpolatedString)
select $"{call.Location}: {call.Name} with an interpolated string loses the structure, pass a template";

// No new HttpClient(), it exhausts sockets
from member in trees.MembersWithBody
from creation in member.ObjectCreations
where creation.TypeName == "HttpClient"
select $"{creation.Location}: use IHttpClientFactory instead of new HttpClient()";

// Every minimal API endpoint requires authorization
from method in trees.Methods
from chain in method.InvocationChains
where chain.Contains("MapGet") && !chain.After("MapGet").Any(c => c.Name == "RequireAuthorization")
select $"{chain.Location}: {chain.First.Arguments[0].StringValue} is not protected";
```

### Models

```csharp
// Records hold no mutable collections, positional properties included
from record in trees.Records
from property in record.Properties
where property.TypeReference.IsMutableCollection
select $"{property.Location}: {record.Name}.{property.Name} is a {property.TypeReference.Name}, use an immutable collection";

// Getters do not throw, whether in a block or as => x ?? throw
from property in trees.Properties
let getter = property.Accessors.Get
where getter is not null
where getter.Throws.Any() || getter.AllExpressions.Any(e => e.Kind == ExpressionKind.Throw)
select $"{property.Location}: the getter of {property.Name} throws, make it a method";
```

### Readability

```csharp
// Early exit instead of else-if ladders: more than two else in one chain
from member in trees.MembersWithBody
from chain in member.IfStatements
where !chain.IsElseIf && chain.ElseCount > 2
select $"{chain.Location}: {member.Name} has an if with {chain.ElseCount} else, use early returns, a switch or a lookup";

// No else after a branch that returns, throws, breaks or continues
from member in trees.MembersWithBody
from statement in member.IfStatements
where statement.HasElse && statement.Then.EndsWithJump
select $"{statement.Location}: the if in {member.Name} returns, drop the else and go on after it";

// Null checks read as null checks: x is null, x is not null
from member in trees.MembersWithBody
from expression in member.AllExpressions
where (expression.Operator is "==" or "!=" && expression.Right?.Kind == ExpressionKind.NullLiteral)
   || expression.Pattern is { Kind: PatternKind.Recursive, IsNotNull: true }
select $"{expression.Location}: write {expression.SourceCode} as is null or is not null";

// A switch expression has a _ arm
from member in trees.MembersWithBody
from switchExpression in member.SwitchExpressions
where !switchExpression.HasDiscardArm
select $"{switchExpression.Location}: {member.Name} switches without a _ arm";

// Parameters are not reassigned
from method in trees.Methods
from assignment in method.AllExpressions
where assignment.Kind == ExpressionKind.Assignment
   && method.Parameters.Any(p => p.Name == assignment.Left?.Name)
select $"{assignment.Location}: do not assign to the parameter {assignment.Left!.Name}";

// Classic extension methods move into C# 14 extension blocks
from type in trees.AllTypes()
where type.HasExtensionMembers
from method in type.Methods
where method.IsExtension && !method.IsInExtensionBlock
select $"{method.Location}: move {method.Name} into an extension block";
```

### Code inside code

A rule walks the code the way you read it: the method, the loop in it, what happens in the loop.
Every statement answers the same questions as its method, only for the code it spans:

```csharp
// No loop inside a loop: use a lookup
from method in trees.Methods
from loop in method.Loops
from inner in loop.Loops
select $"{inner.Location}: {method.Name} runs a {inner.Kind} inside a {loop.Kind}, use a lookup";

// No save inside a loop
from member in trees.MembersWithBody
from loop in member.Loops
from call in loop.Invocations
where call.Name is "SaveChanges" or "SaveChangesAsync"
select $"{call.Location}: {member.Name} calls {call.Name} once per item, save after the loop";

// No await inside a foreach: start the tasks and await them together
from member in trees.MembersWithBody
from loop in member.ForEachLoops
from awaited in loop.Awaits
select $"{awaited.Location}: {member.Name} awaits one at a time, start the tasks and await Task.WhenAll";

// throw; keeps the stack trace, throw ex; does not
from member in trees.MembersWithBody
from tryStatement in member.TryStatements
from @catch in tryStatement.Catches
from throwStatement in @catch.Throws
where @catch.VariableName is not null && throwStatement.Value?.Name == @catch.VariableName
select $"{throwStatement.Location}: {throwStatement.SourceCode} loses the stack trace, write throw;";
```

### With symbols

The syntax sees `Sleep(100)` and cannot tell whose `Sleep` it is. With `ParseMode.WithSymbols` the
compiler can, `using static` and aliases included:

```csharp
// No Thread.Sleep, however it is written
from call in trees.Invocations
where call.Method?.FullyQualifiedName == "System.Threading.Thread.Sleep"
select $"{call.Location}: Thread.Sleep blocks a thread, await Task.Delay instead";

// Tests do not ask for the response to be written
from call in solution.UnitTestTrees.Invocations.Named("AssertPostAsync")
where call.Passes("writeResponse", true)
select $"{call.Location}: writeResponse is true";
```

## What the model gives you

Every declaration carries a `Location`, so a finding points at the source instead of only naming the
type. `Location.ToString()` renders `path(line,column)`, which test runners and IDEs turn into a link:

```csharp
var findings = from property in solution.ProductionTrees.Properties
               where !property.IsReadOnly
               select $"{property.Location}: {property.FullyQualifiedName} is mutable";

// D:\repo\src\Person.cs(12,5): My.Sample.Person.Name is mutable
```

Beyond that, a declaration reports its `Accessibility` (including the default that applies when no
modifier is written), its `Documentation` (the XML comment), its `Modifiers` (now complete, including
`sealed`, `virtual`, `override`, `new`, `async` and `readonly`), its `TypeParameters` with their
constraints, and its `Attributes` split into positional and named arguments. `HasAttribute("Obsolete")`
matches `[Obsolete]`, `[ObsoleteAttribute]` and `[System.ObsoleteAttribute]` alike. When the rule
project can reference the attribute class, `HasAttribute<ObsoleteAttribute>()` and
`Attribute<ObsoleteAttribute>()` take the name from the type instead of a string; the type argument
is constrained to `Attribute`, and the match still goes by name only.

Types also report `Indexers`, `Operators`, `Delegates` and `Finalizers`, and `TypeKind` tells a
`record` from a `record struct`. `tree.Diagnostics` surfaces what Roslyn reported while parsing, so a
rule can assert that no file has a syntax error rather than silently trusting an incomplete model.

## Read it like the code

One file is one `CSharpSyntaxTree`. A rule walks it the way you read the file: its classes, their
methods, the loops in them:

```csharp
var tree = CSharpSyntaxTree.Parse(@"D:\repo\src\Order.cs");

from @class in tree.Classes          // every class of the file, nested ones included
from method in @class.Methods        // the methods of this class
from loop in method.Loops            // the loops of this method
from inner in loop.Loops             // the loops inside this loop
select inner.Location;
```

A file lists everything it declares, nested types included, and a list of files, such as
`solution.ProductionTrees`, has the very same lists for all of them:

```csharp
tree.Classes / Records / Structs / Interfaces / Enums
tree.Methods / Properties / Fields / Constructors
tree.MembersWithBody          // methods, constructors, operators, accessors, local functions: every body
tree.Invocations / LocalVariables

trees.Classes / trees.Methods / trees.MembersWithBody / ...   // the same over many files
```

A type holds what it declares itself; the members of a nested type belong to the nested type:

```csharp
type.Methods            // the methods declared directly in this type
type.NestedTypes        // the types declared directly inside this type
type.AllMethods()       // this type and every type nested in it
type.DescendantTypes()  // every type declared inside this one, at any depth
tree.Types              // the structure as written: the types at file level, holding their nested ones
tree.AllTypes()         // every type of the file regardless of kind, nested ones included
```

The lists are built on first use and then kept: a file is parsed once, so every rule after the first
reads them for free, and `solution.ProductionTrees` hands out the same list every time, so
`solution.ProductionTrees.Methods` is built once as well.

A `partial` class written in several files is in `Classes` once per file, each part where it is
written. For a rule about the class as a whole, `trees.WholeTypes` joins the parts:

```csharp
from type in trees.WholeTypes
where type.Methods.Count > 20
select $"{type.Location}: {type.Name} has {type.Methods.Count} methods in {type.Parts.Count} files";
```

The top level statements of `Program.cs` are in `tree.Statements`, and each of them answers the same
questions as a statement in a method: `Loops`, `Awaits`, `ObjectCreations` and the rest.

Every state is a property, on every type, member and accessor: `IsPublic`, `IsInternal`, `IsPrivate`,
`IsProtected`, `IsStatic`, `IsSealed`, `IsAbstract`, `IsVirtual`, `IsOverride`, `IsPartial`, `IsNew`,
and on the declarations they belong to `method.IsAsync`, `method.IsExtension`, `method.ReturnsTask`,
`field.IsReadOnly`, `parameter.IsOut` and the rest. What needs an argument is a method:
`HasAttribute(name)` or `HasAttribute<TAttribute>()`, `Implements(name)`, `InheritsFrom(name)`.

Every member with a body reports the method calls in it, lambdas included, a local function on its own:

```csharp
method.Invocations                         // Client.AssertPostAsync("..", "..", true)
tree.Invocations.Named("AssertPostAsync")   // Named works on any declaration list
call.Target                                // "Client"
call.Arguments                             // as written, in source order
call.NamedArgument("writeResponse")        // only what is written as writeResponse: true
call.ArgumentFor("writeResponse")          // by position, by name or by default; needs symbols, see below
```

…and the variables declared in it, however they are declared: `var`, `using var`, `for`, `foreach`,
`catch`, `out var`, patterns and deconstruction:

```csharp
method.LocalVariables                      // in source order, lambdas included
variable.Kind                              // Declaration, UsingStatement, ForEach, Catch, Pattern, OutArgument, ...
variable.IsUsing / IsAwaitUsing / IsConst / IsVar
variable.Initializer                       // "factory.GetQueryProvider()"
variable.Invocations                       // the calls inside the initializer
tree.LocalVariables
```

…what it creates, writes and reads:

```csharp
method.ObjectCreations                     // new HttpClient(), new() with the type of its declaration
method.StringLiterals                      // value without quotes, IsInterpolated, IsRaw, EnclosingStatement
method.MemberAccesses                      // DateTime.Now: Target "DateTime", Name "Now", IsInvoked
method.Lambdas                             // parameters, IsAsync, IsStatic, statements, calls
method.Awaits                              // await x, with x as Operand
```

…and its statements, typed, so a rule can pattern match instead of searching text:

```csharp
method.BodyStatements                      // nested: an if holds its branches, a try its blocks
method.AllStatements                       // flat, at any depth, lambdas included
method.TryStatements                       // Block, Catches, Finally, HasCatch, HasFinally
tryStatement.Catches                       // ExceptionType, Filter, IsEmpty, CatchesAnyException, IsRethrowing, IsThrowing
method.SwitchStatements                    // Subject, Sections with Labels (Value, WhenClause), HasDefault
method.SwitchExpressions                   // Arms with Pattern, TypedPattern, WhenClause, Result; HasDiscardArm
method.IfStatements / Returns / Throws / UsingStatements / LockStatements
method.Loops / ForLoops / ForEachLoops / WhileLoops / DoWhileLoops
loop.IsFor / IsForEach / IsWhile / IsDoWhile / IsAwait
ifStatement.ElseCount / ElseIfs / IsElseIf // the else-if chain, counted once from its first if
statement.EndsWithJump                     // ends in return, throw, break, continue or goto
statement.Invocations                      // the calls inside one statement
statement.Expressions                      // what the statement holds: condition, value, initializers
tree.Statements                            // top level statements, typed the same way

from tryStatement in method.TryStatements
from @catch in tryStatement.Catches
where @catch.CatchesAnyException && @catch.IsEmpty
select $"{@catch.Location}: an empty catch swallows every exception";

from tryStatement in method.TryStatements
from @catch in tryStatement.Catches
where @catch.CatchesAnyException && !@catch.IsThrowing && !@catch.Invocations.Any(i => i.Name == "LogError")
select $"{@catch.Location}: the catch neither throws nor logs";
```

A statement asks the same as its member, for the code inside it: `loop.Loops`, `loop.Awaits`,
`ifStatement.Throws`, `tryStatement.Block.Invocations`, `tryStatement.Finally?.Invocations`. A catch
does too, for its block: `@catch.Returns`, `@catch.Invocations`, and so does a switch section for the code
after its labels: `section.Throws`, `section.Invocations`. A catch is part of its `try`, not a
statement, so it is reached the way the code reads: the method, its `try`, its catches. The lists are
`AllStatements`, the shortcuts above, `AllExpressions`, `Invocations`, `Awaits`, `LocalVariables`,
`ObjectCreations`, `StringLiterals`, `MemberAccesses`, `Lambdas` and `SwitchExpressions`; like the
member's they include what sits in lambdas and leave local functions to themselves. Each list is built
on first use and then kept, so asking twice costs nothing.

Expressions are classified wherever they are passed or assigned: call and attribute arguments,
initializers and default values. `Kind` tells a string literal from a `nameof`, a `typeof`, a lambda or
a member access, and `StringValue` is the text without quotes:

```csharp
call.Arguments[0].Kind                     // ExpressionKind.StringLiteral
call.Arguments[0].StringValue              // "api/users"
call.Arguments[1].ValueExpression.TypeReference   // Foo in typeof(Foo)
argument.ValueExpression.TextWithHoles()   // "v{}/users" for $"v{version}/users"
attribute.PositionalExpressions / NamedArguments[i].ValueExpression
field.InitializerExpression.HasNullForgiving      // null!
```

An expression is a tree. A binary expression or an assignment has `Left`, `Right` and `Operator`, a
call `Name`, `Target` and `Arguments`, `a ? b : c` its `Condition`, `WhenTrue` and `WhenFalse`, and
`x is ...` its `Pattern`. `Children` and `DescendantsAndSelf()` walk it, `statement.Expressions` hands out
what a statement holds, and `method.AllExpressions` is every expression of a body at any depth:

```csharp
from method in tree.Methods
from comparison in method.AllExpressions
where comparison.Operator is "==" or "!=" && comparison.Right?.Kind == ExpressionKind.NullLiteral
select $"{comparison.Location}: write {comparison.Left!.SourceCode} is null";

from method in tree.Methods
from assignment in method.AllExpressions
where assignment.Kind == ExpressionKind.Assignment
   && method.Parameters.Any(p => p.Name == assignment.Left?.Name)
select $"{assignment.Location}: do not assign to the parameter {assignment.Left!.Name}";
```

A pattern is taken apart the same way, wherever it is written: `expression.Pattern`,
`label.TypedPattern` and `arm.TypedPattern`. `Kind` is a `Constant`, `Type`, `Declaration`, `Relational`,
`Not`, `And`, `Or`, `Recursive` (with `Properties` and `Positional`), `List` and the like; `IsNull` and
`IsNotNull` cover the null checks.

```csharp
pattern.Properties[0].Name                 // "Customer.Age" in { Customer.Age: >= 18 }
pattern.Properties[0].Pattern.Operator     // ">="
```

Accessors carry their body like a method does: `property.Accessors.Get.Invocations`,
`indexer.Accessors.Set.AllStatements`, `event.Add.LocalVariables`. `MembersWithBody`, and with it
`Invocations` and `LocalVariables` of a file, includes them. A C# 14 `extension(string value) { }` block of a
static class is in `type.ExtensionBlocks`, with its `Receiver`, `Methods` and `Properties`; `tree.Methods`
and `tree.Properties` include its members.

`method.IsExtension` is true for an extension method written either way, `Shout(this string value)` or
inside `extension(string value) { }`; `ExtensionReceiver` is the parameter it extends and
`IsInExtensionBlock` tells the two forms apart. With symbols, `call.Method.IsExtension` works for
methods from packages and the framework too, `Enumerable.Select` for one.

```csharp
from type in tree.AllTypes()
where type.HasExtensionMembers
from method in type.Methods
where method.IsExtension && !method.IsInExtensionBlock
select $"{method.Location}: move {method.Name} into an extension block";
```

Fluent calls are linked: `call.Receiver` is the call it is made on, and `method.InvocationChains`
groups them:

```csharp
from chain in method.InvocationChains
where chain.Contains("MapGet") && !chain.After("MapGet").Any(c => c.Name == "RequireAuthorization")
select $"{chain.Location}: {chain.First.Arguments[0].StringValue} is not protected";
```

## Types

Every type written in a declaration is also taken apart, so a rule no longer cuts `Task<List<Foo>>` at
`<` and `>` by hand. `Type` and `ReturnParameter` stay the text; `TypeReference` and `ReturnType` are
the structure:

```csharp
field.TypeReference.Is("ILogger")                    // matches ILogger<MyService> as well
method.ReturnType.UnwrapTask().IsCollection          // Task<List<Foo>> -> true
method.ReturnType.UnwrapTask().ElementType?.Name     // "Foo"
property.TypeReference.TypeArguments                 // each one a TypeReference again
parameter.TypeReference.Mentions("DateTime")         // anywhere inside, List<DateTime?> included
variable.TypeReference.IsVar
TypeReference.Parse("Dictionary<string, int>")       // the same for a type written as text
```

`Is` compares the way a rule means it: without type arguments it matches any, a namespace only has to
agree when both sides write one, `String` equals `string`, and the `?` only counts when you ask for it.
It goes by the written name, so a class of your own named `List` counts as a collection as well.

A collection is exactly one of `IsImmutableCollection` (`ImmutableList`, `FrozenSet`, ...),
`IsReadOnlyCollection` (`IReadOnlyList`, `IEnumerable`, ...) and `IsMutableCollection` (`List`, arrays,
`Dictionary`, ...). `IsImmutable` goes all the way down: `ImmutableList<string>` is immutable,
`ImmutableList<List<int>>` is not. Your own types are unknown to the syntax and count as not immutable
until you say otherwise. A positional record lists its parameters as `Properties` too, marked
`IsPositional`, so `record Order(List<Line> Lines)` is found:

```csharp
var immutableRecords = trees.Records.Select(r => r.Name).ToHashSet();

from type in trees.Records
from property in type.Properties
where !property.TypeReference.IsImmutableWith(t => immutableRecords.Contains(t.Name))
select $"{property.Location}: {type.Name}.{property.Name} can change";
```

## Symbols

The syntax alone cannot tell that `true` in `Client.AssertPostAsync("..", "..", true)` is the
parameter `writeResponse`, least of all when the method comes from a NuGet package. The compiler can.
The parse mode of the solution decides which of the two you get, over the same records:

```csharp
var solution = SolutionFile.Find("MySolution.sln", ParseMode.SyntaxOnly);   // syntax only, parsed once and cached
var solution = SolutionFile.Find("MySolution.sln", ParseMode.WithSymbols);  // plus a Roslyn compilation per project, built on first use

var findings = from call in solution.UnitTestTrees.Invocations.Named("AssertPostAsync")
               where call.Passes("writeResponse", true)
               select $"{call.Location}: writeResponse is true";
```

The code hangs on the solution and its projects: `solution.AllTrees`, `solution.ProductionTrees`,
`solution.UnitTestTrees`, `project.Trees`, `solution.CompilationDiagnostics()` and
`project.CompilationDiagnostics()`. A solution parsed without a mode gets the syntax on first use.

What a resolved call tells, in the vocabulary of the syntax model:

```csharp
call.Resolution            // Resolved | Ambiguous | Unresolved
call.Method                // a Method record; for code of the solution the very record the tree holds
call.Method.Origin         // Source | Package("AspNetCore.Simple.MsTest.Sdk", "9.5.15") | Framework
call.BoundArguments        // every parameter with what is passed for it, in parameter order
call.ArgumentFor("writeResponse").Expression    // "true", by position or by name alike
call.ArgumentFor("writeResponse").IsExplicit    // false when left out and the default applies
call.ArgumentFor("writeResponse").Is(true)      // literal, const or default value alike
call.Passes("writeResponse", true)              // the same in one go, false for an unresolved call
call.Candidates            // the competing overloads of an ambiguous call
call.Roslyn                // node, semantic model, symbol and operation, for anything not covered
```

The compilation is built from what restore and build left on disk: package assemblies from
`obj/project.assets.json`, the framework reference packs, project references as compilations, the
generated global usings and XAML output from obj, and the source generators of frameworks and packages.
The solution therefore has to be restored, which it is whenever its tests run. `project.CompilationDiagnostics()`
shows what the compiler could not resolve.

With `ParseMode.SyntaxOnly` a symbol access throws instead of guessing, so the cost of a rule stays
visible: nothing is compiled unless a rule asks for symbols. `CodeBase.FromSource(code).WithSymbols()`
gives the same API for code in memory, which is handy for trying a rule out;
`CodeBase.FromSources(("A.cs", a), ("B.cs", b))` takes several files.

## XAML

`.xaml` files parse into the same shape of model, so a rule can reach across markup and code behind:

```csharp
var tree = XamlSyntaxTree.Parse(@"D:\repo\src\MainWindow.xaml");

tree.FullyQualifiedName          // "My.Sample.MainWindow", the x:Class
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
tree.AllTemplates()             // DataTemplate, ControlTemplate, ItemsPanelTemplate, …
tree.AllBindings()              // every Binding, nested ones included
tree.AllMarkupExtensions()      // every {…}, nested ones included
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

Upgrading from an older major? See **[MIGRATION.md](MIGRATION.md)** — it covers the mechanical
renames and, more importantly, the changes that still compile but report something different. It is
written so you can hand it to an AI assistant along with your rule suite.

The short version for 7.0:

| 6.x | 7.0 |
|---|---|
| `method.IsAsync()`, guessed from the return type's name | `method.IsAsync` for the `async` modifier, `method.ReturnType.IsTask` for an awaitable return |
| `method.UseCorrectAsyncNaming()` | `method.Name.EndsWith("Async")` |
| calls in accessors and extension blocks were invisible | `tree.Invocations` and friends include them, so rules may report more |
| positional record parameters were missing from `Properties` | they are in it, marked `IsPositional`, so rules may report more |
| `RecordExtensions`, `AddCodeFormatter`, `FormatSyntaxTree` | removed, they generated code rather than parsed it |
| `tree.Classes`, `Records`, ... held the types at file level only | they hold every type of the file, nested ones included; `tree.Types` is the file level |
| `trees.AllMethods()`, `AllMembersWithBody()`, `AllInvocations()`, ... | removed; `trees.Methods`, `trees.MembersWithBody`, `trees.Invocations`, ... as properties |

The short version for 6.0, all of it caught by the compiler:

| 5.x | 6.0 |
|---|---|
| `FullQualifiedName`, `NameSpace` | `FullyQualifiedName`, `Namespace` |
| `declaration.SyntaxTree` (a `string`) | `declaration.SourceCode` |
| `ProductiveProjects`, `ProductiveTrees` | `ProductionProjects`, `ProductionTrees` |
| `call.Argument("x")` | `call.ArgumentFor("x")`, or `call.Passes("x", value)` |
| `fileInfo.Value.FullName`, `package.PackageId.Value` | `fileInfo.FullName`, `package.Id` |
| `project.ProjectFileInfo.Value.FullName` | `project.Path` |
| `new SolutionFileName(x).FindSolutionFileReverseFrom(dir).Parse()` | `SolutionFile.Find(x)`, with `dir` as optional start |
| `new SolutionFileInfo(path).Parse()`, `csharpFileInfo.Parse()` | `SolutionFile.Parse(path)`, `CSharpSyntaxTree.Parse(path)` |
| `solution.SolutionFileInfo`, `project.ProjectFileInfo` | `solution.FileInfo`, `project.FileInfo`, a plain `FileInfo` |
| `attributes.Named(x)` returned the first match | it returns all of them; `declaration.Attribute(x)` for one |

And for 5.0:

| Before | Now |
|---|---|
| `type.Methods` returned nested types' methods too | `type.AllMethods()` |
| `tree.Classes` contained nested classes | `tree.Types` for the types at file level; since 7.0 `tree.Classes` holds every class of the file again |
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
| attached property named `RowProperty` | `Name` is `Row`, `FullyQualifiedName` is `Grid.Row`, `DependencyPropertyName` is `RowProperty` |
| `x:Name` and `Name` were both called `Name` | `Property.Prefix` and `Property.IsXamlDirective` tell them apart |
| `Window : UserControl`, `DynamicResource : StaticResource` | siblings, with `RootKind` and a shared `ResourceReference` |
| `DataTemplate`, `ResourceDictionary`, `ResourceDictionaryControl` | `Template`, `ResourceDictionaryRoot`, `ResourceDictionaryElement` |
| `new Control(...)` positional | object initializer, `new Control { TypeName = ..., ... }` |
| an unreadable value threw and lost the file | `tree.Diagnostics` |

## License

Copyright 2022-2026 (c) Rene Peuser.

Licensed under the [Apache License 2.0](License.txt).
