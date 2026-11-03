using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// One parsed C# file, read the way you read the file: <see cref="Classes"/>, <see cref="Records"/>,
    /// <see cref="Methods"/>, <see cref="Properties"/> and the other lists hold everything the file
    /// declares, nested types included. <see cref="Types"/> is the structure as written: the types at
    /// file level, each holding its nested types.
    /// </summary>
    [DebuggerDisplay("{FileName}")]
    public record CSharpSyntaxTree
    {
        /// <summary>Reads and parses the C# file at <paramref name="path"/>.</summary>
        public static CSharpSyntaxTree Parse(string path)
        {
            Throw.IfNullOrWhiteSpace(path);

            return Parse(new FileInfo(path));
        }

        /// <summary>Reads and parses the given C# file.</summary>
        public static CSharpSyntaxTree Parse(FileInfo fileInfo)
        {
            return CSharpParser.Parse(fileInfo);
        }

        public required Namespace Namespace { get; init; }

        public required string FileName { get; init; }

        public ImmutableList<Using> Usings { get; init; } = ImmutableList<Using>.Empty;

        /// <summary>
        /// The types declared at file level, in source order, regardless of kind; each holds its nested
        /// types in <see cref="TypeDeclaration.NestedTypes"/>. For every type of the file at any depth use
        /// <see cref="Classes"/>, <see cref="Records"/> and the like, or <c>AllTypes()</c>.
        /// </summary>
        public ImmutableList<TypeDeclaration> Types { get; init; } = ImmutableList<TypeDeclaration>.Empty;

        public ImmutableList<Delegate> Delegates { get; init; } = ImmutableList<Delegate>.Empty;

        /// <summary>The top level statements of the file, empty unless it is the program entry file.</summary>
        public ImmutableList<Statement> Statements { get; init; } = ImmutableList<Statement>.Empty;

        /// <summary>Assembly and module level attributes, as in <c>[assembly: InternalsVisibleTo(...)]</c>.</summary>
        public ImmutableList<Attribute> AssemblyAttributes { get; init; } = ImmutableList<Attribute>.Empty;

        /// <summary>What Roslyn reported while parsing, see <see cref="ParseDiagnostic"/>.</summary>
        public ImmutableList<ParseDiagnostic> Diagnostics { get; init; } = ImmutableList<ParseDiagnostic>.Empty;

        /// <summary>The source of the whole file.</summary>
        public required string SourceCode { get; init; }

        /// <summary>Every namespace declared in the file. A file may hold more than one.</summary>
        public ImmutableList<Namespace> Namespaces { get; init; } = ImmutableList<Namespace>.Empty;

        /// <summary>Every class of the file, nested ones included, in source order. A <c>record class</c> is in <see cref="Records"/>.</summary>
        /// <example>
        /// <code>
        /// from @class in tree.Classes
        /// from method in @class.Methods
        /// from loop in method.Loops
        /// select loop.Location
        /// </code>
        /// </example>
        public ImmutableList<Class> Classes => Views.Classes;

        /// <summary>Every record of the file, <c>record struct</c> and nested ones included.</summary>
        public ImmutableList<Record> Records => Views.Records;

        /// <summary>Every interface of the file, nested ones included.</summary>
        public ImmutableList<Interface> Interfaces => Views.Interfaces;

        /// <summary>Every enum of the file, nested ones included.</summary>
        public ImmutableList<Enum> Enums => Views.Enums;

        /// <summary>Every struct of the file, nested ones included. A <c>record struct</c> is in <see cref="Records"/>.</summary>
        public ImmutableList<Struct> Structs => Views.Structs;

        /// <summary>Every method of every type in the file, those of nested types and extension blocks included.</summary>
        public ImmutableList<Method> Methods => Views.Methods;

        /// <summary>Every property of every type in the file, positional ones and those of nested types and extension blocks included.</summary>
        public ImmutableList<Property> Properties => Views.Properties;

        /// <summary>Every field of every type in the file, those of nested types included.</summary>
        public ImmutableList<Field> Fields => Views.Fields;

        /// <summary>Every constructor of every type in the file, primary constructors and those of nested types included.</summary>
        public ImmutableList<Constructor> Constructors => Views.Constructors;

        /// <summary>
        /// Everything in the file that can hold code: methods, constructors, operators, finalizers, the
        /// accessors of properties, indexers and events, and the local functions inside them. The
        /// starting point for a rule about what happens inside a body, whatever kind of member it is.
        /// </summary>
        public ImmutableList<MemberWithBody> MembersWithBody => Views.MembersWithBody;

        /// <summary>Every method call in the file, in every member and local function of every type.</summary>
        public ImmutableList<Invocation> Invocations => Views.Invocations;

        /// <summary>Every local variable in the file, in every member and local function of every type.</summary>
        public ImmutableList<LocalVariable> LocalVariables => Views.LocalVariables;

        public bool HasParseErrors => Diagnostics.Any(d => d.IsError);

        /// <summary>Every type of the file at any depth, see <c>AllTypes()</c>.</summary>
        internal ImmutableList<TypeDeclaration> AllTypesOfFile => Views.Types;

        private Unequated<Cached<TreeViews>> _views;

        /// <summary>Built once per file on first use; a file is parsed once, so every rule after the first reads the lists for free.</summary>
        private TreeViews Views => Cache.For(ref _views, this, static tree => new TreeViews(tree));
    }

    /// <summary>The declarations of one file at any depth, each list built on first use and then kept.</summary>
    internal sealed class TreeViews(CSharpSyntaxTree tree)
    {
        internal ImmutableList<TypeDeclaration> Types => field ??= tree.Types.SelectMany(QueryExtensions.SelfAndDescendants).ToImmutableList();

        internal ImmutableList<Class> Classes => field ??= Types.OfType<Class>().ToImmutableList();

        internal ImmutableList<Record> Records => field ??= Types.OfType<Record>().ToImmutableList();

        internal ImmutableList<Interface> Interfaces => field ??= Types.OfType<Interface>().ToImmutableList();

        internal ImmutableList<Enum> Enums => field ??= Types.OfType<Enum>().ToImmutableList();

        internal ImmutableList<Struct> Structs => field ??= Types.OfType<Struct>().ToImmutableList();

        internal ImmutableList<Method> Methods => field ??= Types.SelectMany(QueryExtensions.MethodsOf).ToImmutableList();

        internal ImmutableList<Property> Properties => field ??= Types.SelectMany(QueryExtensions.PropertiesOf).ToImmutableList();

        internal ImmutableList<Field> Fields => field ??= Types.SelectMany(t => t.Fields).ToImmutableList();

        internal ImmutableList<Constructor> Constructors => field ??= Types.SelectMany(t => t.Constructors).ToImmutableList();

        internal ImmutableList<MemberWithBody> MembersWithBody => field ??= Types.SelectMany(QueryExtensions.AllMembersWithBody).ToImmutableList();

        internal ImmutableList<Invocation> Invocations => field ??= MembersWithBody.SelectMany(m => m.Invocations).ToImmutableList();

        internal ImmutableList<LocalVariable> LocalVariables => field ??= MembersWithBody.SelectMany(m => m.LocalVariables).ToImmutableList();
    }
}
