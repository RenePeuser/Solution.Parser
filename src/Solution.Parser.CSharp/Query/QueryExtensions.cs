using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Navigation and predicates for writing code rules. Member lists on a type hold only what that
    /// type declares; the methods here are how you opt into walking nested types as well.
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>Every type of the file, including nested ones at any depth.</summary>
        public static ImmutableList<TypeDeclaration> AllTypes(this CSharpSyntaxTree syntaxTree)
        {
            return syntaxTree.AllTypesOfFile;
        }

        /// <summary>Every type of the files, including nested ones at any depth.</summary>
        public static ImmutableList<TypeDeclaration> AllTypes(this IEnumerable<CSharpSyntaxTree> syntaxTrees)
        {
            return syntaxTrees.SelectMany(tree => tree.AllTypesOfFile).ToImmutableList();
        }

        /// <summary>The types declared inside this type, at any depth, excluding the type itself.</summary>
        public static ImmutableList<TypeDeclaration> DescendantTypes(this TypeDeclaration type)
        {
            return type.NestedTypes.SelectMany(SelfAndDescendants).ToImmutableList();
        }

        /// <summary>This type followed by every type declared inside it, at any depth.</summary>
        public static ImmutableList<TypeDeclaration> SelfAndDescendantTypes(this TypeDeclaration type)
        {
            return SelfAndDescendants(type).ToImmutableList();
        }

        /// <summary>
        /// The methods of this type and of every type nested in it. This is what the member lists
        /// returned before nesting was handled correctly.
        /// </summary>
        public static ImmutableList<Method> AllMethods(this TypeDeclaration type)
        {
            return SelfAndDescendants(type).SelectMany(MethodsOf).ToImmutableList();
        }

        public static ImmutableList<Property> AllProperties(this TypeDeclaration type)
        {
            return SelfAndDescendants(type).SelectMany(PropertiesOf).ToImmutableList();
        }

        internal static IEnumerable<Method> MethodsOf(TypeDeclaration type)
        {
            return type.ExtensionBlocks.IsEmpty ? type.Methods : type.Methods.Concat(type.ExtensionBlocks.SelectMany(b => b.Methods));
        }

        internal static IEnumerable<Property> PropertiesOf(TypeDeclaration type)
        {
            return type.ExtensionBlocks.IsEmpty ? type.Properties : type.Properties.Concat(type.ExtensionBlocks.SelectMany(b => b.Properties));
        }

        public static ImmutableList<Field> AllFields(this TypeDeclaration type)
        {
            return SelfAndDescendants(type).SelectMany(t => t.Fields).ToImmutableList();
        }

        /// <summary>
        /// Every member of the type that can carry a body: methods, constructors, operators,
        /// finalizers, the accessors of properties, indexers and events, the members of extension
        /// blocks and the local functions declared inside all of them.
        /// </summary>
        public static ImmutableList<MemberWithBody> AllMembersWithBody(this TypeDeclaration type)
        {
            IEnumerable<MemberWithBody> members =
            [
                ..type.Methods,
                ..type.Constructors,
                ..type.Operators,
                ..type.Finalizers,
                ..type.Properties.SelectMany(p => p.Accessors.All),
                ..type.Indexers.SelectMany(i => i.Accessors.All),
                ..type.Events.SelectMany(e => new[] { e.Add, e.Remove }.OfType<Accessor>()),
                ..type.ExtensionBlocks.SelectMany(b => b.Methods),
                ..type.ExtensionBlocks.SelectMany(b => b.Operators),
                ..type.ExtensionBlocks.SelectMany(b => b.Properties).SelectMany(p => p.Accessors.All)
            ];

            return members.SelectMany(m => m.LocalFunctions.Prepend<MemberWithBody>(m)).ToImmutableList();
        }

        /// <summary>
        /// The declarations with exactly this name, for example <c>tree.Methods.Named("Dispose")</c> or
        /// <c>tree.Invocations.Named("AssertPostAsync")</c>.
        /// </summary>
        public static ImmutableList<T> Named<T>(this IEnumerable<T> declarations, string name)
            where T : DeclarationBase
        {
            return declarations.Where(d => d.Name.Equals(name, StringComparison.Ordinal)).ToImmutableList();
        }

        /// <summary>Matches with or without the <c>Attribute</c> suffix, see <see cref="Attribute.IsNamed"/>.</summary>
        public static bool HasAttribute(this DeclarationWithModifiers declaration, string attributeName)
        {
            return declaration.Attributes.Any(a => a.IsNamed(attributeName));
        }

        public static Attribute? Attribute(this DeclarationWithModifiers declaration, string attributeName)
        {
            return declaration.Attributes.FirstOrDefault(a => a.IsNamed(attributeName));
        }

        /// <summary>
        /// Like <see cref="HasAttribute(DeclarationWithModifiers, string)"/> with the name taken from the
        /// attribute class, so <c>record.HasAttribute&lt;DynamoDBPropertyAttribute&gt;()</c> replaces
        /// <c>record.HasAttribute(nameof(DynamoDBPropertyAttribute))</c>. The match still goes by name only,
        /// the namespace of the class is not compared.
        /// </summary>
        public static bool HasAttribute<TAttribute>(this DeclarationWithModifiers declaration)
            where TAttribute : System.Attribute
        {
            return declaration.HasAttribute(CSharp.Attribute.NameOf<TAttribute>());
        }

        public static Attribute? Attribute<TAttribute>(this DeclarationWithModifiers declaration)
            where TAttribute : System.Attribute
        {
            return declaration.Attribute(CSharp.Attribute.NameOf<TAttribute>());
        }

        /// <summary>
        /// True when the base list names the given type. Base classes and interfaces share one list,
        /// because telling them apart needs the semantic model, so this matches either.
        /// </summary>
        public static bool InheritsFrom(this TypeDeclaration type, string typeName)
        {
            return type.BaseTypes.Any(b => b.Matches(typeName));
        }

        /// <summary>
        /// True when the base list names the given interface. Shares its implementation with
        /// <see cref="InheritsFrom"/>; the two names exist so a rule can read the way it means.
        /// </summary>
        public static bool Implements(this TypeDeclaration type, string interfaceName)
        {
            return type.BaseTypes.Any(b => b.Matches(interfaceName));
        }

        /// <summary>
        /// <see cref="InheritsFrom(TypeDeclaration, string)"/> with the type as a type argument, as in
        /// <c>type.InheritsFrom&lt;ControllerBase&gt;()</c>. Matches like <see cref="TypeReference.Is{T}"/>, so
        /// a closed generic is exact: <c>InheritsFrom&lt;Collection&lt;int&gt;&gt;()</c> does not match
        /// <c>Collection&lt;string&gt;</c>.
        /// </summary>
        public static bool InheritsFrom<TBase>(this TypeDeclaration type)
            where TBase : class
        {
            return type.BaseTypes.Any(b => b.TypeReference.Is<TBase>());
        }

        /// <summary>
        /// <see cref="Implements(TypeDeclaration, string)"/> with the interface as a type argument, as in
        /// <c>type.Implements&lt;IDisposable&gt;()</c>. Matches like <see cref="TypeReference.Is{T}"/>, so
        /// <c>Implements&lt;IEquatable&lt;Money&gt;&gt;()</c> does not match <c>IEquatable&lt;Order&gt;</c>.
        /// </summary>
        /// <exception cref="ArgumentException">The type argument is no interface; use <see cref="InheritsFrom{TBase}"/>.</exception>
        public static bool Implements<TInterface>(this TypeDeclaration type)
            where TInterface : class
        {
            if (!typeof(TInterface).IsInterface)
            {
                throw new ArgumentException($"'{typeof(TInterface).Name}' is no interface. Use InheritsFrom<{typeof(TInterface).Name}>() for a base class.", nameof(TInterface));
            }

            return type.BaseTypes.Any(b => b.TypeReference.Is<TInterface>());
        }

        /// <summary>
        /// Matches the written name against the given one, ignoring a namespace qualifier and matching
        /// an open generic name such as <c>IEnumerable</c> against <c>IEnumerable&lt;int&gt;</c>.
        /// </summary>
        private static bool Matches(this BaseType baseType, string typeName)
        {
            return Simplify(baseType.TypeName).Equals(Simplify(typeName), StringComparison.Ordinal) ||
                   baseType.TypeName.Equals(typeName, StringComparison.Ordinal);
        }

        private static string Simplify(string typeName)
        {
            var generic = typeName.IndexOf('<');
            var withoutGeneric = generic < 0 ? typeName : typeName[..generic];
            var lastDot = withoutGeneric.LastIndexOf('.');

            return lastDot < 0 ? withoutGeneric : withoutGeneric[(lastDot + 1)..];
        }

        internal static IEnumerable<TypeDeclaration> SelfAndDescendants(TypeDeclaration type)
        {
            yield return type;

            foreach (var nested in type.NestedTypes)
            {
                foreach (var descendant in SelfAndDescendants(nested))
                {
                    yield return descendant;
                }
            }
        }
    }
}
