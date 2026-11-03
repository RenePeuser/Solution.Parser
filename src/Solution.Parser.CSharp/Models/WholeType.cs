using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A type as the compiler sees it: every part of a <c>partial</c> type together, however many files
    /// it is written in. <c>trees.Classes</c> lists each part where it is written, which is what a
    /// finding should point at; a rule about the type as a whole, how many methods it has, counts here.
    /// </summary>
    /// <example>
    /// <code>
    /// from type in solution.ProductionTrees.WholeTypes
    /// where type.Methods.Count > 20
    /// select $"{type.Location}: {type.FullyQualifiedName} has {type.Methods.Count} methods in {type.Parts.Count} files";
    /// </code>
    /// </example>
    [DebuggerDisplay("{Kind} {FullyQualifiedName} ({Parts.Count} parts)")]
    public sealed record WholeType
    {
        /// <summary>The parts, in the order the files were given; a type that is not partial has exactly one.</summary>
        public required ImmutableList<TypeDeclaration> Parts { get; init; }

        public string Name => Parts[0].Name;

        public string FullyQualifiedName => Parts[0].FullyQualifiedName;

        public TypeKind Kind => Parts[0].Kind;

        /// <summary>Where the first part is written, so a finding about the whole type has a place to point at.</summary>
        public CodeLocation Location => Parts[0].Location;

        /// <summary>True when the type is declared <c>partial</c>, whether its parts are spread over files or not.</summary>
        public bool IsPartial => Parts.Any(p => p.IsPartial);

        /// <summary>
        /// True when any part is written <c>public</c>; the compiler applies an accessibility written on
        /// one part to the whole type, and the parts that leave it out read as the default internal.
        /// </summary>
        public bool IsPublic => Parts.Any(p => p.IsPublic);

        /// <summary>True when any part is written <c>static</c>, which makes the whole type static.</summary>
        public bool IsStatic => Parts.Any(p => p.IsStatic);

        /// <summary>True when any part is written <c>abstract</c>, which makes the whole type abstract.</summary>
        public bool IsAbstract => Parts.Any(p => p.IsAbstract);

        /// <summary>True when any part is written <c>sealed</c>, which makes the whole type sealed.</summary>
        public bool IsSealed => Parts.Any(p => p.IsSealed);

        public ImmutableList<Method> Methods => Of(p => p.Methods);

        public ImmutableList<Property> Properties => Of(p => p.Properties);

        public ImmutableList<Field> Fields => Of(p => p.Fields);

        public ImmutableList<Constructor> Constructors => Of(p => p.Constructors);

        public ImmutableList<Event> Events => Of(p => p.Events);

        public ImmutableList<Indexer> Indexers => Of(p => p.Indexers);

        public ImmutableList<Operator> Operators => Of(p => p.Operators);

        public ImmutableList<TypeDeclaration> NestedTypes => Of(p => p.NestedTypes);

        public ImmutableList<Attribute> Attributes => Of(p => p.Attributes);

        /// <summary>The base class and interfaces of every part, each named once.</summary>
        public ImmutableList<BaseType> BaseTypes => Parts.SelectMany(p => p.BaseTypes).DistinctBy(b => b.TypeName).ToImmutableList();

        /// <summary>Everything in every part that can hold code, see <c>tree.MembersWithBody</c>.</summary>
        public ImmutableList<MemberWithBody> MembersWithBody => Of(QueryExtensions.AllMembersWithBody);

        private ImmutableList<T> Of<T>(System.Func<TypeDeclaration, IEnumerable<T>> members)
        {
            return Parts.Count == 1 ? members(Parts[0]).ToImmutableList() : Parts.SelectMany(members).ToImmutableList();
        }

        /// <summary>
        /// Puts the parts of each partial type together. Only types declared <c>partial</c> are joined,
        /// and only when kind, name and number of type parameters agree, so two unrelated types of the
        /// same name, in two projects, stay two types.
        /// </summary>
        internal static ImmutableList<WholeType> Join(IEnumerable<TypeDeclaration> types)
        {
            var wholes = new List<List<TypeDeclaration>>();
            var partials = new Dictionary<(TypeKind, string, int), List<TypeDeclaration>>();

            foreach (var type in types)
            {
                if (!type.IsPartial)
                {
                    wholes.Add([type]);

                    continue;
                }

                var key = (type.Kind, type.FullyQualifiedName, type.TypeParameters.Count);

                if (partials.TryGetValue(key, out var parts))
                {
                    parts.Add(type);
                }
                else
                {
                    partials[key] = parts = [type];
                    wholes.Add(parts);
                }
            }

            return wholes.Select(parts => new WholeType { Parts = parts.ToImmutableList() }).ToImmutableList();
        }
    }
}
