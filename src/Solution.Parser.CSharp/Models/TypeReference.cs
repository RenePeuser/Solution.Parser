using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A type as written in the source, taken apart: <c>Task&lt;List&lt;Foo&gt;?&gt;</c> is a <c>Task</c>
    /// whose one type argument is a nullable <c>List</c> of <c>Foo</c>. It saves a rule from cutting the
    /// type string at <c>&lt;</c> and <c>&gt;</c> by hand.
    /// </summary>
    /// <remarks>
    /// This is what the syntax tells, nothing more. A <c>List</c> is recognised by its name, so a class
    /// of your own that happens to be called <c>List</c> counts as a collection as well, and <c>var</c>
    /// stays <c>var</c>. Which type is really meant needs symbols, see the semantic package.
    /// </remarks>
    /// <example>
    /// <code>
    /// method.ReturnType.UnwrapTask().IsCollection    // Task&lt;List&lt;Foo&gt;&gt; -&gt; true
    /// field.TypeReference.Is("ILogger")             // matches ILogger&lt;MyService&gt; as well
    /// parameter.TypeReference.Mentions("DateTime")  // List&lt;DateTime?&gt; -&gt; true
    /// </code>
    /// </example>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record TypeReference
    {
        /// <summary>Stands for a type that is not written, as for the parameter of <c>x =&gt; x * 2</c>.</summary>
        public static TypeReference None { get; } = new() { Name = string.Empty, SourceCode = string.Empty };

        private static readonly ConcurrentDictionary<string, TypeReference> Patterns = new(StringComparer.Ordinal);

        /// <summary>
        /// The name without namespace and without type arguments: <c>List</c> for
        /// <c>System.Collections.Generic.List&lt;int&gt;</c>, <c>int</c> for <c>int?</c>. An array is named
        /// after its element with the brackets, <c>int[]</c>; a tuple by its source, <c>(int, string)</c>.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// What is written in front of the name, <c>System.Collections.Generic</c> in
        /// <c>System.Collections.Generic.List&lt;int&gt;</c>; null when the name stands alone, which is the
        /// usual case thanks to using directives. A <c>global::</c> prefix is left out.
        /// </summary>
        public string? Qualifier { get; init; }

        /// <summary>The type arguments in source order, <c>string</c> and <c>int</c> in <c>Dictionary&lt;string, int&gt;</c>.</summary>
        public ImmutableList<TypeReference> TypeArguments { get; init; } = ImmutableList<TypeReference>.Empty;

        /// <summary>
        /// True when this very type carries the <c>?</c>. In <c>List&lt;int?&gt;</c> the list is not
        /// nullable, its type argument is.
        /// </summary>
        public bool IsNullable { get; init; }

        /// <summary>The number of dimensions of an array, 2 for <c>int[,]</c>; 0 when this is not an array.</summary>
        public int ArrayRank { get; init; }

        /// <summary>The element of an array, <c>int</c> for <c>int[]</c> and <c>int[]</c> for <c>int[][]</c>.</summary>
        public TypeReference? ArrayElementType { get; init; }

        /// <summary>The elements of a tuple type such as <c>(int Id, string Name)</c>; empty for any other type.</summary>
        public ImmutableList<TupleElement> TupleElements { get; init; } = ImmutableList<TupleElement>.Empty;

        /// <summary>True for a C# keyword type: <c>int</c>, <c>string</c>, <c>object</c>, <c>void</c> and the like.</summary>
        public bool IsPredefined { get; init; }

        /// <summary>The type exactly as written, <c>?</c> and qualifier included.</summary>
        public required string SourceCode { get; init; }

        /// <summary>Where the type is written. <see cref="CodeLocation.None"/> for a type made by <see cref="Parse"/>.</summary>
        public CodeLocation Location { get; init; } = CodeLocation.None;

        /// <summary>The qualifier and the name, <c>System.Collections.Generic.List</c>; just the name when nothing is written in front of it.</summary>
        public string FullName => Qualifier is null ? Name : $"{Qualifier}.{Name}";

        public bool IsGeneric => !TypeArguments.IsEmpty;

        public bool IsArray => ArrayRank > 0;

        public bool IsTuple => !TupleElements.IsEmpty;

        /// <summary>True for a <c>var</c>, where the compiler infers the type.</summary>
        public bool IsVar => Name == "var" && Qualifier is null && !IsPredefined && !IsGeneric;

        public bool IsVoid => IsPredefined && Name == "void";

        /// <summary>True for <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c> and <c>ValueTask&lt;T&gt;</c>.</summary>
        public bool IsTask => !IsArray && !IsTuple && Name is "Task" or "ValueTask" && TypeArguments.Count <= 1;

        /// <summary>
        /// True for an array and for the well known collection types of the framework: <c>IEnumerable</c>,
        /// <c>List</c>, <c>HashSet</c>, <c>IReadOnlyList</c>, the immutable, frozen and concurrent ones and
        /// the dictionaries. A <c>string</c> is not a collection here.
        /// </summary>
        public bool IsCollection => IsArray || (!IsTuple && (Sequences.Contains(Name) || Dictionaries.Contains(Name)));

        /// <summary>True for <c>Dictionary</c>, <c>IDictionary</c>, <c>IReadOnlyDictionary</c> and their immutable, frozen and concurrent siblings.</summary>
        public bool IsDictionary => !IsArray && !IsTuple && Dictionaries.Contains(Name);

        /// <summary>
        /// True for a collection that never changes once built: <c>ImmutableList</c>,
        /// <c>ImmutableArray</c>, <c>ImmutableDictionary</c> and their siblings, the <c>IImmutable</c>
        /// interfaces, <c>FrozenSet</c> and <c>FrozenDictionary</c>. What it holds may still change, see
        /// <see cref="IsImmutable"/>.
        /// </summary>
        public bool IsImmutableCollection => !IsArray && !IsTuple && ImmutableCollections.Contains(Name);

        /// <summary>
        /// True for a read only view: <c>IEnumerable</c>, <c>IReadOnlyList</c>, <c>IReadOnlyCollection</c>,
        /// <c>IReadOnlySet</c>, <c>IReadOnlyDictionary</c>, <c>ReadOnlyCollection</c> and the like. The
        /// holder of the reference cannot change it, but whoever handed it out may, as a
        /// <c>List</c> behind an <c>IReadOnlyList</c>.
        /// </summary>
        public bool IsReadOnlyCollection => !IsArray && !IsTuple && ReadOnlyCollections.Contains(Name);

        /// <summary>
        /// True for a collection that can be changed through the reference: an array, <c>List</c>,
        /// <c>Dictionary</c>, <c>HashSet</c>, <c>IList</c>, <c>ICollection</c>, <c>ObservableCollection</c>,
        /// the concurrent ones. Every collection is exactly one of mutable, read only and immutable.
        /// </summary>
        public bool IsMutableCollection => IsCollection && !IsImmutableCollection && !IsReadOnlyCollection;

        /// <summary>
        /// True when a value of the type can never change, all the way down: a C# keyword type but
        /// <c>object</c>, the well known immutable framework types such as <c>Guid</c>, <c>DateTime</c>,
        /// <c>TimeSpan</c> and <c>Uri</c>, and an immutable collection, a tuple, a <c>Nullable</c> or a
        /// <c>KeyValuePair</c> of such types. <c>ImmutableList&lt;string&gt;</c> is immutable,
        /// <c>ImmutableList&lt;List&lt;int&gt;&gt;</c> is not.
        /// </summary>
        /// <remarks>
        /// The syntax does not know your own types, so false means "not known to be immutable": an
        /// <c>ImmutableList&lt;Order&gt;</c> is false even when <c>Order</c> is an immutable record. Tell
        /// it which of your types are immutable with <see cref="IsImmutableWith"/>.
        /// </remarks>
        public bool IsImmutable => IsImmutableWith(static _ => false);

        /// <summary>
        /// <see cref="IsImmutable"/>, with <paramref name="isImmutableType"/> deciding for every type the
        /// parser does not know, at any depth.
        /// </summary>
        /// <example>
        /// <code>
        /// var immutableRecords = solution.ProductionTrees.AllTypes().OfType&lt;Record&gt;().Where(IsImmutableRecord).Select(r =&gt; r.Name).ToHashSet();
        /// property.TypeReference.IsImmutableWith(t =&gt; immutableRecords.Contains(t.Name))
        /// </code>
        /// </example>
        public bool IsImmutableWith(Func<TypeReference, bool> isImmutableType)
        {
            Throw.IfNull(isImmutableType);

            if (IsTuple)
            {
                return TupleElements.All(e => e.Type.IsImmutableWith(isImmutableType));
            }

            if (IsArray)
            {
                return false;
            }

            if (IsPredefined)
            {
                return Name is not ("object" or "void");
            }

            if (IsImmutableCollection || ImmutableWrappers.Contains(Name))
            {
                return TypeArguments.All(a => a.IsImmutableWith(isImmutableType));
            }

            if (IsCollection)
            {
                return false;
            }

            return (!IsGeneric && ImmutableScalars.Contains(Name)) || isImmutableType(this);
        }

        /// <summary>
        /// What the collection holds: the element of an array or the one type argument of a collection,
        /// <c>Foo</c> for <c>IReadOnlyList&lt;Foo&gt;</c>. Null for anything else, also for a dictionary,
        /// whose key and value are <c>TypeArguments[0]</c> and <c>TypeArguments[1]</c>.
        /// </summary>
        public TypeReference? ElementType => IsArray
                                                 ? ArrayElementType
                                                 : !IsTuple && Sequences.Contains(Name) && TypeArguments.Count == 1
                                                     ? TypeArguments[0]
                                                     : null;

        /// <summary>
        /// What awaiting the type gives: <c>Foo</c> for <c>Task&lt;Foo&gt;</c>, <c>void</c> for a plain
        /// <c>Task</c>, and the type itself when it is no task at all. So
        /// <c>method.ReturnType.UnwrapTask()</c> is the result type, async or not.
        /// </summary>
        public TypeReference UnwrapTask()
        {
            if (!IsTask)
            {
                return this;
            }

            return TypeArguments.Count == 1 ? TypeArguments[0] : Parse("void");
        }

        /// <summary>
        /// True when this is the given type. The comparison goes by what is written and is forgiving
        /// where the syntax cannot know better:
        /// <list type="bullet">
        /// <item><c>Is("List")</c> matches any <c>List&lt;T&gt;</c>; <c>Is("List&lt;int&gt;")</c> only that one, and <c>Is("Dictionary&lt;,&gt;")</c> any dictionary.</item>
        /// <item>A namespace only has to agree when both sides write one, so <c>Is("System.Guid")</c> matches <c>Guid</c>.</item>
        /// <item>A keyword equals its framework name, so <c>Is("String")</c> matches <c>string</c>.</item>
        /// <item>The <c>?</c> only counts when you ask for it: <c>Is("int")</c> matches <c>int?</c>, <c>Is("int?")</c> does not match <c>int</c>.</item>
        /// </list>
        /// </summary>
        public bool Is(string typeName)
        {
            Throw.IfNullOrWhiteSpace(typeName);

            return Matches(this, Patterns.GetOrAdd(typeName, Parse));
        }

        /// <summary>
        /// True when the given type appears anywhere in this one, the type itself included, so
        /// <c>Task&lt;List&lt;DateTime&gt;&gt;</c> mentions <c>Task</c>, <c>List</c> and <c>DateTime</c>. Matches like <see cref="Is"/>.
        /// </summary>
        public bool Mentions(string typeName)
        {
            return SelfAndDescendants().Any(t => t.Is(typeName));
        }

        /// <summary>This type followed by every type written inside it: type arguments, array elements and tuple elements, at any depth.</summary>
        public IEnumerable<TypeReference> SelfAndDescendants()
        {
            yield return this;

            var children = TypeArguments.Concat(TupleElements.Select(e => e.Type));

            if (ArrayElementType is not null)
            {
                children = children.Prepend(ArrayElementType);
            }

            foreach (var descendant in children.SelectMany(c => c.SelfAndDescendants()))
            {
                yield return descendant;
            }
        }

        /// <summary>
        /// Takes a type written as text apart, the same way the parser does it with the types in the
        /// source. Handy to build an expected value, as in <c>TypeReference.Parse("Task&lt;int&gt;")</c>.
        /// </summary>
        public static TypeReference Parse(string typeName)
        {
            Throw.IfNull(typeName);

            return SyntaxFactory.ParseTypeName(typeName).ToTypeReference(filePath: null);
        }

        public override string ToString()
        {
            return SourceCode;
        }

        private static bool Matches(TypeReference actual, TypeReference pattern)
        {
            if (pattern.IsNullable && !actual.IsNullable)
            {
                return false;
            }

            if (pattern.IsArray || actual.IsArray)
            {
                return pattern.IsArray && actual.IsArray &&
                       pattern.ArrayRank == actual.ArrayRank &&
                       Matches(actual.ArrayElementType!, pattern.ArrayElementType!);
            }

            if (pattern.IsTuple || actual.IsTuple)
            {
                return pattern.IsTuple && actual.IsTuple &&
                       pattern.TupleElements.Count == actual.TupleElements.Count &&
                       pattern.TupleElements.Zip(actual.TupleElements).All(p => Matches(p.Second.Type, p.First.Type));
            }

            // An omitted type argument, as in Dictionary<,>, stands for any type.
            if (pattern.Name.Length == 0)
            {
                return true;
            }

            if (!FrameworkName(actual.Name).Equals(FrameworkName(pattern.Name), StringComparison.Ordinal))
            {
                return false;
            }

            if (pattern.Qualifier is not null && actual.Qualifier is not null &&
                !pattern.Qualifier.Equals(actual.Qualifier, StringComparison.Ordinal))
            {
                return false;
            }

            return pattern.TypeArguments.IsEmpty ||
                   (pattern.TypeArguments.Count == actual.TypeArguments.Count &&
                    pattern.TypeArguments.Zip(actual.TypeArguments).All(p => Matches(p.Second, p.First)));
        }

        private static string FrameworkName(string name)
        {
            return Keywords.TryGetValue(name, out var frameworkName) ? frameworkName : name;
        }

        private static readonly FrozenDictionary<string, string> Keywords = new Dictionary<string, string>
        {
            ["bool"] = "Boolean",
            ["byte"] = "Byte",
            ["sbyte"] = "SByte",
            ["char"] = "Char",
            ["decimal"] = "Decimal",
            ["double"] = "Double",
            ["float"] = "Single",
            ["int"] = "Int32",
            ["uint"] = "UInt32",
            ["nint"] = "IntPtr",
            ["nuint"] = "UIntPtr",
            ["long"] = "Int64",
            ["ulong"] = "UInt64",
            ["short"] = "Int16",
            ["ushort"] = "UInt16",
            ["object"] = "Object",
            ["string"] = "String",
            ["void"] = "Void"
        }.ToFrozenDictionary(StringComparer.Ordinal);

        private static readonly FrozenSet<string> Sequences = new[]
        {
            "IEnumerable", "ICollection", "IList", "IReadOnlyCollection", "IReadOnlyList", "ISet", "IReadOnlySet",
            "List", "HashSet", "SortedSet", "LinkedList", "Queue", "Stack", "PriorityQueue",
            "Collection", "ReadOnlyCollection", "ObservableCollection", "ReadOnlyObservableCollection",
            "ImmutableArray", "ImmutableList", "ImmutableHashSet", "ImmutableSortedSet", "ImmutableQueue", "ImmutableStack",
            "IImmutableList", "IImmutableSet", "IImmutableQueue", "IImmutableStack",
            "FrozenSet", "ConcurrentBag", "ConcurrentQueue", "ConcurrentStack", "BlockingCollection", "IProducerConsumerCollection"
        }.ToFrozenSet(StringComparer.Ordinal);

        private static readonly FrozenSet<string> ImmutableCollections = new[]
        {
            "ImmutableArray", "ImmutableList", "ImmutableHashSet", "ImmutableSortedSet", "ImmutableQueue", "ImmutableStack",
            "IImmutableList", "IImmutableSet", "IImmutableQueue", "IImmutableStack",
            "ImmutableDictionary", "ImmutableSortedDictionary", "IImmutableDictionary",
            "FrozenSet", "FrozenDictionary"
        }.ToFrozenSet(StringComparer.Ordinal);

        private static readonly FrozenSet<string> ReadOnlyCollections = new[]
        {
            "IEnumerable", "IReadOnlyCollection", "IReadOnlyList", "IReadOnlySet", "IReadOnlyDictionary",
            "ReadOnlyCollection", "ReadOnlyObservableCollection", "ReadOnlyDictionary"
        }.ToFrozenSet(StringComparer.Ordinal);

        /// <summary>Framework types whose values never change; the keyword types are recognised by <see cref="IsPredefined"/>.</summary>
        private static readonly FrozenSet<string> ImmutableScalars = new[]
        {
            "String", "Boolean", "Byte", "SByte", "Char", "Decimal", "Double", "Single", "Half",
            "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "Int128", "UInt128", "IntPtr", "UIntPtr", "BigInteger",
            "Guid", "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly", "TimeSpan", "TimeZoneInfo",
            "Uri", "Version", "Type", "Index", "Range"
        }.ToFrozenSet(StringComparer.Ordinal);

        /// <summary>Generic framework types that are immutable when their type arguments are.</summary>
        private static readonly FrozenSet<string> ImmutableWrappers = new[]
        {
            "Nullable", "KeyValuePair", "Tuple", "ValueTuple"
        }.ToFrozenSet(StringComparer.Ordinal);

        private static readonly FrozenSet<string> Dictionaries = new[]
        {
            "IDictionary", "IReadOnlyDictionary", "Dictionary", "SortedDictionary", "SortedList", "ReadOnlyDictionary",
            "ConcurrentDictionary", "ImmutableDictionary", "ImmutableSortedDictionary", "IImmutableDictionary", "FrozenDictionary"
        }.ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>One element of a tuple type, <c>int Id</c> in <c>(int Id, string Name)</c>.</summary>
    [DebuggerDisplay("{Type} {Name}")]
    public sealed record TupleElement(string? Name, TypeReference Type);
}
