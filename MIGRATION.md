# Migrating to Solution.Parser 5.0

This release changes behaviour that older versions got wrong. Some of it the compiler catches. Some
of it does not, and that part silently changes what your code rules report.

## For the AI assistant doing this migration

Work through the phases in order. Do not skip phase 3 — it is the only one the compiler cannot help
with, and it is the one that changes results.

1. **Phase 1** — update the package reference.
2. **Phase 2** — mechanical renames. Apply all of them; the build tells you when you are done.
3. **Phase 3** — silent behaviour changes. **Read every rule that touches the listed members and
   decide per rule.** Code that still compiles here may now report a different number of findings.
4. **Phase 4** — construction changed, only relevant if you build the models yourself.
5. **Phase 5** — verify.

Rules about how to report back:
- When a rule's finding count changes, do not "fix" it by loosening the rule. In almost every case
  the old count was wrong. Say which rule changed, from what to what, and why.
- Do not introduce `AllMethods()` / `AllTypes()` everywhere just to keep old numbers. Pick the one
  that matches what the rule means. Phase 3.1 explains the choice.
- If a rule becomes trivially true or trivially false after the change, it was probably broken
  before. Flag it rather than deleting it.

---

## Phase 1 — Package

One package, as before:

```xml
<PackageReference Include="Solution.Parser" Version="5.0.0" />
```

It now contains several assemblies (`Solution.Parser.CSharp`, `.Xaml`, `.Project`, `.Sln`,
`.Nuspec`, `.AspNet`, `.Core`). Nothing to change in the project file — but the namespaces moved, see
phase 2.

---

## Phase 2 — Mechanical renames

The compiler catches all of these.

### 2.1 Namespaces

| Before | Now |
|---|---|
| `Solution.Parser.Common` | `Solution.Parser.Core` |
| `Solution.Parser.Solution` | `Solution.Parser.Sln` |
| `Solution.Parser.XAML` | `Solution.Parser.Xaml` |
| `Solution.Parser.CSharp` | unchanged |

### 2.2 Types and members

| Before | Now |
|---|---|
| `XAMLFileInfo` | `XamlFileInfo` |
| `project.CSharpFileInfos` | `project.SourceFiles.CSharpFiles()` |
| `project.XAMLFileInfos` | `project.SourceFiles.XamlFiles()` |
| `class.Interfaces` | `class.BaseTypes`, or `class.Implements("IFoo")` |
| `element.Controls` (XAML) | `element.Children` |
| `element.LineNumber` (XAML) | `element.Location` |
| `element.DataTemplates` (XAML) | `tree.AllTemplates()` / `tree.AllDataTemplates()` |

`SourceFiles` is a plain `ImmutableList<FileInfo>`; `CSharpFiles()` and `XamlFiles()` are extension
methods on `IEnumerable<FileInfo>`, so they need the matching `using`:

```csharp
using Solution.Parser.CSharp;   // for CSharpFiles()
using Solution.Parser.Xaml;     // for XamlFiles()

var csharp = project.SourceFiles.CSharpFiles();
```

### 2.3 Type hierarchy

`Record` no longer derives from `Class`, `Class` no longer derives from `Interface`, `Struct` no
longer derives from `Class`. They are now siblings below `TypeDeclaration`.

| Before | Now |
|---|---|
| `if (x is Interface)` matched classes too | it matches interfaces only |
| `if (x is Class)` matched records and structs too | it matches classes only |
| no common base | `TypeDeclaration`, with `Kind` of `TypeKind` |

```csharp
// any declared type, regardless of kind
foreach (TypeDeclaration type in tree.AllTypes())
{
    if (type.Kind == TypeKind.RecordStruct) { }
}
```

`tree.Classes`, `.Records`, `.Interfaces`, `.Structs`, `.Enums` still exist as filtered views over
`tree.Types`. Same for `NestedClasses`, `NestedStructs`, `NestedInterfaces`, `NestedEnums` over
`NestedTypes`.

### 2.4 XAML types

| Before | Now |
|---|---|
| `Window : UserControl` | siblings below `Root`, told apart by `RootKind` |
| `DynamicResource : StaticResource` | siblings, with a shared `ResourceReference` |
| `DataTemplate` | `Template`, with `Kind` |
| `ResourceDictionary` | `ResourceDictionaryRoot` |
| `ResourceDictionaryControl` | `ResourceDictionaryElement` |

---

## Phase 3 — Silent behaviour changes

**This is the part that matters.** Everything here still compiles. It just means something else now.

### 3.1 Member lists hold only what a type declares itself

Before, every member list was built from a recursive walk that crossed type boundaries. A type
reported the members of its nested types as its own, and a nested type appeared again at file level.

```csharp
class Outer { void A(); class Inner { void B(); } }
```

| Expression | Before | Now |
|---|---|---|
| `tree.Classes` | `Outer`, `Inner` | `Outer` |
| `outer.Methods` | `A`, `B` | `A` |
| `outer.NestedClasses` | every nested type at any depth | direct children only |

**Decide per rule which one it means:**

```csharp
type.Methods         // the methods this type declares          <- usually what a rule means
type.AllMethods()    // this type and every type nested in it   <- the old behaviour
tree.AllTypes()      // every type of the file, nested included
tree.AllMethods()    // every method of the file
type.DescendantTypes()        // nested types at any depth, excluding the type itself
type.SelfAndDescendantTypes() // the type and all of them
```

A rule like "a type must not have more than 20 methods" wants `type.Methods` — with the old
behaviour a class with several small nested types could fail for no reason. A rule like "no method
may be named `Foo`" wants `tree.AllMethods()`, which is what it effectively got before.

There are equivalents across a whole solution:

```csharp
ProductiveCode.AllTypes()   // IEnumerable<CSharpSyntaxTree>
```

### 3.2 `SyntaxTree` on a member is the member, not the file

`Method`, `Field`, `Event`, `EventField` and `EnumField` returned **the source of the whole file**
from `SyntaxTree`. They now return their own source.

```csharp
method.SyntaxTree   // before: the whole file.  now: "public void Go() { }"
tree.SyntaxTree     // the whole file, if that is what you want
```

Any rule doing `method.SyntaxTree.Split(...)`, `.Contains(...)` or a regex over it was scanning the
entire file per member. Those rules changed both in speed and in result. A line-counting rule in
particular was measuring the file, not the member — use `method.LineCount` now.

`Class`, `Record`, `Struct`, `Interface`, `Enum` and `Property` were already correct and did not
change. `Method.MethodValue` and `Method.MethodBody` keep their old meaning.

### 3.3 A shared field declaration reports every variable

```csharp
private int _a, _b, _c;
```

Before: one `Field`, named `_a`. Now: three. A rule over field naming now sees `_b` and `_c` for the
first time and may report new findings. `field.IsPartOfMultiVariableDeclaration` tells you they
share a declaration.

### 3.4 Modifiers that used to go missing

`sealed`, `virtual`, `override`, `new`, `async`, `extern`, `unsafe`, `volatile`, `file`, `fixed`,
`ref`, `in`, `out`, `scoped` were dropped. `readonly` was recognised on fields only, so a
`readonly struct` looked mutable.

Any rule filtering on those now sees declarations it never saw before. Predicates:

```csharp
declaration.IsSealed() / IsVirtual() / IsOverride() / IsAbstract() / IsStatic() / IsPartial()
```

### 3.5 `IsNullable` reads the type, not the text

`Property.IsNullable` was `Type.Contains('?')`. `List<int?>` therefore counted as nullable. It now
reflects the annotation on the property type itself.

### 3.6 `IsReadOnly` reads the accessors, not the text

`Property.IsReadOnly` was `!source.Contains("set;")`. A setter with a block (`set { }`) was missed
entirely; so was an expression-bodied one.

The meaning is unchanged on purpose: **read only means there is no `set` accessor, and `init` still
counts as read only** — that is what an immutability rule asks about. New, if you need the
distinction: `IsInitOnly`, `HasGetter`, `HasSetter`, `Accessors.Get/Set/Init`.

### 3.7 `IsAsync`

`Method.IsAsync` now reports the `async` **modifier**. The old extension method, which guessed from
`ReturnParameter.Contains("Task")`, still exists and still behaves as before. A method returning
`Task` without `async` is no longer reported as async by the property.

### 3.8 `Statements` means top-level statements

`tree.Statements` returned every statement found anywhere in the file. It now holds the top-level
statements of the file, which is empty unless it is the entry-point file.

### 3.9 Accessibility has a default

`declaration.Accessibility` resolves the language default when no modifier is written: a class
member is `Private`, an interface member is `Public`, a type in a namespace is `Internal`. Before,
an absent modifier was indistinguishable from an explicit `private`. A rule such as "no public
fields" is only writable now — and will find things.

### 3.10 XAML: the element tree

| Expression | Before | Now |
|---|---|---|
| `element.Controls` | every descendant, each duplicated | `element.Children`, direct children only |
| `element.Parent` | always `null` | the element this one is written in |
| `element.Styles` | always empty | `tree.AllStyles()` |
| `element.DataContext` | set on every element | `null` unless the element sets one |
| `<Grid.RowDefinitions>` | a child element named `Grid.RowDefinitions` | a `PropertyElement` in `element.PropertyElements` |
| attached property `Grid.Row` | named `RowProperty` | `Name` is `Row`, `FullQualifiedName` is `Grid.Row`, `DependencyPropertyName` is `RowProperty` |
| `x:Name` vs `Name` | both called `Name` | `Property.Prefix` and `Property.IsXamlDirective` tell them apart |

Any rule that walked `Controls` was seeing duplicates and will now report fewer findings. The
recursive walk is available explicitly:

```csharp
tree.AllElements() / DescendantElements() / SelfAndDescendantElements()
tree.AllStyles() / AllSetters() / AllTriggers() / AllBindings() / AllResources()
tree.AllTemplates() / AllDataTemplates() / AllMarkupExtensions()
element.Ancestors() / AncestorsAndSelf()
tree.FindByName("x") / FindByKey("x") / OfTypeName("Button")
```

A XAML file that could not be fully read used to throw and lose the whole file. It now parses as far
as it can and reports the rest on `tree.Diagnostics` / `tree.HasDiagnostics`.

---

## Phase 4 — Construction

Only relevant if you build model instances yourself, for example in tests. The models are records
with init properties instead of positional parameters:

```csharp
// before
new Class(nameSpace, name, modifiers, /* ... 17 more ... */);

// now
new Class { Name = name, FullQualifiedName = fqn, NameSpace = ns, SyntaxTree = src, FilePath = path };
```

`Name`, `FullQualifiedName`, `SyntaxTree`, `FilePath` and (on a type) `NameSpace` are `required`;
everything else defaults to empty. Same change on the XAML side (`new Control { TypeName = ... }`).

---

## Phase 5 — Verify

1. Build. Phase 2 is done when it compiles.
2. Run the rule suite and **compare the finding count of every rule against the previous run.**
   Changes are expected — sections 3.1 through 3.9 each cause some. What you want is to be able to
   name the reason for each one.
3. Look for rules that now report *zero* findings. A rule that never fails proves nothing, and a
   rule that was passing only because of a bug is worth knowing about.
4. `tree.HasParseErrors` is new. Worth one rule of its own:

```csharp
[TestMethod]
public void No_File_Has_Syntax_Errors()
{
    var broken = ProductiveCode.Where(t => t.HasParseErrors)
                               .Select(t => $"{t.FileName}: {t.Diagnostics.First(d => d.IsError).Message}");

    Assert.IsFalse(broken.Any(), string.Join(Environment.NewLine, broken));
}
```

---

## What you can use now that you could not before

Every declaration carries a `Location` that renders as `path(line,column)`, which test runners and
IDEs turn into a clickable link:

```csharp
var findings = from tree in ProductiveCode
               from type in tree.AllTypes()
               from property in type.Properties
               where !property.IsReadOnly
               select $"{property.Location}: {type.FullQualifiedName}.{property.Name} is mutable";

// D:\repo\src\Person.cs(12,5): My.Sample.Person.Name is mutable
```

Also new on declarations: `Accessibility`, `Documentation` (the XML comment, with `Summary`,
`Returns`, `Parameters`, `IsDocumented`), `TypeParameters` with their constraints, `LineCount`, and
attributes split into `PositionalArguments` and `NamedArguments`. `HasAttribute("Obsolete")` matches
`[Obsolete]`, `[ObsoleteAttribute]` and `[System.ObsoleteAttribute]` alike.

Types report `Indexers`, `Operators`, `Delegates` and `Finalizers`, which were dropped entirely
before. Methods report `IsExpressionBodied`, `IsIterator`, `IsPartialDefinition` and
`ExplicitInterfaceSpecifier`. Usings report `IsGlobal`, `IsStatic` and `Alias`. Files report
`AssemblyAttributes` and `NameSpaces`.
