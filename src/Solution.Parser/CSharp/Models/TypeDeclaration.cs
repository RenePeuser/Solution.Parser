using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The shared shape of every declared type. <see cref="Class"/>, <see cref="Struct"/>,
    /// <see cref="Record"/>, <see cref="Interface"/> and <see cref="Enum"/> are siblings below it, so
    /// <c>is</c> checks and pattern matching mean what they say and <c>tree.AllTypes()</c> can hand back
    /// all of them in one list.
    /// </summary>
    /// <remarks>
    /// Every member list holds the members declared directly in this type. Members of a nested type
    /// belong to that nested type; use the <c>Descendant</c> and <c>All</c> query extensions to walk
    /// the whole tree.
    /// </remarks>
    [DebuggerDisplay("{Kind} {Name}")]
    public abstract record TypeDeclaration : DeclarationWithModifiers
    {
        public required NameSpace NameSpace { get; init; }

        /// <summary>Distinguishes class, struct, record, record struct, interface and enum.</summary>
        public abstract TypeKind Kind { get; }

        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        /// <summary>
        /// Everything on the base list, in source order: the base class, if any, followed by the
        /// implemented interfaces. Telling the two apart needs the semantic model, which this parser
        /// deliberately does not build, so both live in one list. Use <c>InheritsFrom</c> and
        /// <c>Implements</c> to query it.
        /// </summary>
        public ImmutableList<BaseType> BaseTypes { get; init; } = ImmutableList<BaseType>.Empty;

        public ImmutableList<Constructor> Constructors { get; init; } = ImmutableList<Constructor>.Empty;

        public ImmutableList<Property> Properties { get; init; } = ImmutableList<Property>.Empty;

        public ImmutableList<Method> Methods { get; init; } = ImmutableList<Method>.Empty;

        public ImmutableList<Field> Fields { get; init; } = ImmutableList<Field>.Empty;

        public ImmutableList<Event> Events { get; init; } = ImmutableList<Event>.Empty;

        public ImmutableList<EventField> EventFields { get; init; } = ImmutableList<EventField>.Empty;

        public ImmutableList<Indexer> Indexers { get; init; } = ImmutableList<Indexer>.Empty;

        public ImmutableList<Operator> Operators { get; init; } = ImmutableList<Operator>.Empty;

        public ImmutableList<Delegate> Delegates { get; init; } = ImmutableList<Delegate>.Empty;

        public ImmutableList<Finalizer> Finalizers { get; init; } = ImmutableList<Finalizer>.Empty;

        /// <summary>The types declared directly inside this type.</summary>
        public ImmutableList<TypeDeclaration> NestedTypes { get; init; } = ImmutableList<TypeDeclaration>.Empty;

        /// <summary>
        /// The parameters of the primary constructor when there is one, otherwise those of the
        /// constructor taking the most arguments.
        /// </summary>
        public ImmutableList<Parameter> Parameters { get; init; } = ImmutableList<Parameter>.Empty;

        public Constructor? PrimaryConstructor => Constructors.FirstOrDefault(c => c.IsPrimary);

        public bool IsGeneric => !TypeParameters.IsEmpty;

        public ImmutableList<Class> NestedClasses => NestedTypes.OfType<Class>().ToImmutableList();

        public ImmutableList<Struct> NestedStructs => NestedTypes.OfType<Struct>().ToImmutableList();

        public ImmutableList<Record> NestedRecords => NestedTypes.OfType<Record>().ToImmutableList();

        public ImmutableList<Interface> NestedInterfaces => NestedTypes.OfType<Interface>().ToImmutableList();

        public ImmutableList<Enum> NestedEnums => NestedTypes.OfType<Enum>().ToImmutableList();
    }
}
