using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Many files read like one: <c>solution.ProductionTrees.Classes</c> is every class of the
    /// production code, <c>.Methods</c> every method, nested types included, the same lists a single
    /// file has.
    /// </summary>
    /// <example>
    /// <code>
    /// var trees = solution.ProductionTrees;
    ///
    /// from method in trees.Methods
    /// from loop in method.Loops
    /// from inner in loop.Loops
    /// select $"{inner.Location}: {method.Name} nests loops";
    /// </code>
    /// </example>
    public static class SyntaxTreeListExtensions
    {
        // Keyed by the list itself: an immutable list never changes, so what was found in it stays true.
        private static readonly ConditionalWeakTable<ImmutableList<CSharpSyntaxTree>, TreeListViews> Views = new();

        extension(IEnumerable<CSharpSyntaxTree> trees)
        {
            /// <summary>Every class of the files, nested ones included. A <c>record class</c> is in <c>Records</c>.</summary>
            public ImmutableList<Class> Classes => ViewsOf(trees).Classes;

            /// <summary>Every record of the files, <c>record struct</c> and nested ones included.</summary>
            public ImmutableList<Record> Records => ViewsOf(trees).Records;

            /// <summary>Every interface of the files, nested ones included.</summary>
            public ImmutableList<Interface> Interfaces => ViewsOf(trees).Interfaces;

            /// <summary>Every enum of the files, nested ones included.</summary>
            public ImmutableList<Enum> Enums => ViewsOf(trees).Enums;

            /// <summary>Every struct of the files, nested ones included. A <c>record struct</c> is in <c>Records</c>.</summary>
            public ImmutableList<Struct> Structs => ViewsOf(trees).Structs;

            /// <summary>Every method of every type in the files, those of nested types and extension blocks included.</summary>
            public ImmutableList<Method> Methods => ViewsOf(trees).Methods;

            /// <summary>Every property of every type in the files, positional ones and those of nested types and extension blocks included.</summary>
            public ImmutableList<Property> Properties => ViewsOf(trees).Properties;

            /// <summary>Every field of every type in the files, those of nested types included.</summary>
            public ImmutableList<Field> Fields => ViewsOf(trees).Fields;

            /// <summary>Every constructor of every type in the files, primary constructors and those of nested types included.</summary>
            public ImmutableList<Constructor> Constructors => ViewsOf(trees).Constructors;

            /// <summary>
            /// Everything in the files that can hold code: methods, constructors, operators, finalizers,
            /// accessors and the local functions inside them. The starting point for a rule about what
            /// happens inside a body, whatever kind of member it is.
            /// </summary>
            public ImmutableList<MemberWithBody> MembersWithBody => ViewsOf(trees).MembersWithBody;

            /// <summary>Every method call in the files, in every member and local function of every type.</summary>
            public ImmutableList<Invocation> Invocations => ViewsOf(trees).Invocations;

            /// <summary>Every local variable in the files, in every member and local function of every type.</summary>
            public ImmutableList<LocalVariable> LocalVariables => ViewsOf(trees).LocalVariables;

            /// <summary>Every <c>#pragma warning disable</c> and <c>restore</c> of the files, in file and source order.</summary>
            public ImmutableList<PragmaWarning> PragmaWarnings => ViewsOf(trees).PragmaWarnings;

            /// <summary>
            /// Every type of the files as the compiler sees it: the parts of a <c>partial</c> type, written in
            /// several files, joined into one. <c>Classes</c> and the like list each part on its own.
            /// </summary>
            /// <example>
            /// <code>
            /// from type in trees.WholeTypes
            /// where type.Methods.Count > 20
            /// select $"{type.Location}: {type.Name} has {type.Methods.Count} methods";
            /// </code>
            /// </example>
            public ImmutableList<WholeType> WholeTypes => ViewsOf(trees).WholeTypes;
        }

        private static TreeListViews ViewsOf(IEnumerable<CSharpSyntaxTree> trees)
        {
            Throw.IfNull(trees);

            // Anything but an immutable list may change between two calls, so it is not remembered.
            return trees is ImmutableList<CSharpSyntaxTree> list
                       ? Views.GetValue(list, static l => new TreeListViews(l))
                       : new TreeListViews(trees.ToImmutableList());
        }
    }

    /// <summary>The lists of several files joined, each built on first use out of the lists every file keeps.</summary>
    internal sealed class TreeListViews(ImmutableList<CSharpSyntaxTree> trees)
    {
        internal ImmutableList<Class> Classes => field ??= trees.SelectMany(t => t.Classes).ToImmutableList();

        internal ImmutableList<Record> Records => field ??= trees.SelectMany(t => t.Records).ToImmutableList();

        internal ImmutableList<Interface> Interfaces => field ??= trees.SelectMany(t => t.Interfaces).ToImmutableList();

        internal ImmutableList<Enum> Enums => field ??= trees.SelectMany(t => t.Enums).ToImmutableList();

        internal ImmutableList<Struct> Structs => field ??= trees.SelectMany(t => t.Structs).ToImmutableList();

        internal ImmutableList<Method> Methods => field ??= trees.SelectMany(t => t.Methods).ToImmutableList();

        internal ImmutableList<Property> Properties => field ??= trees.SelectMany(t => t.Properties).ToImmutableList();

        internal ImmutableList<Field> Fields => field ??= trees.SelectMany(t => t.Fields).ToImmutableList();

        internal ImmutableList<Constructor> Constructors => field ??= trees.SelectMany(t => t.Constructors).ToImmutableList();

        internal ImmutableList<MemberWithBody> MembersWithBody => field ??= trees.SelectMany(t => t.MembersWithBody).ToImmutableList();

        internal ImmutableList<Invocation> Invocations => field ??= trees.SelectMany(t => t.Invocations).ToImmutableList();

        internal ImmutableList<LocalVariable> LocalVariables => field ??= trees.SelectMany(t => t.LocalVariables).ToImmutableList();

        internal ImmutableList<PragmaWarning> PragmaWarnings => field ??= trees.SelectMany(t => t.PragmaWarnings).ToImmutableList();

        internal ImmutableList<WholeType> WholeTypes => field ??= WholeType.Join(trees.SelectMany(t => t.AllTypesOfFile));
    }
}
