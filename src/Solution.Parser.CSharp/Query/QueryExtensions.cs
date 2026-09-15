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
            return syntaxTree.Types.SelectMany(SelfAndDescendants).ToImmutableList();
        }

        /// <summary>Every type of the files, including nested ones at any depth.</summary>
        public static ImmutableList<TypeDeclaration> AllTypes(this IEnumerable<CSharpSyntaxTree> syntaxTrees)
        {
            return syntaxTrees.SelectMany(tree => tree.Types).SelectMany(SelfAndDescendants).ToImmutableList();
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
            return SelfAndDescendants(type).SelectMany(t => t.Methods).ToImmutableList();
        }

        public static ImmutableList<Method> AllMethods(this CSharpSyntaxTree syntaxTree)
        {
            return syntaxTree.AllTypes().SelectMany(t => t.Methods).ToImmutableList();
        }

        public static ImmutableList<Property> AllProperties(this TypeDeclaration type)
        {
            return SelfAndDescendants(type).SelectMany(t => t.Properties).ToImmutableList();
        }

        public static ImmutableList<Property> AllProperties(this CSharpSyntaxTree syntaxTree)
        {
            return syntaxTree.AllTypes().SelectMany(t => t.Properties).ToImmutableList();
        }

        public static ImmutableList<Field> AllFields(this TypeDeclaration type)
        {
            return SelfAndDescendants(type).SelectMany(t => t.Fields).ToImmutableList();
        }

        public static ImmutableList<Field> AllFields(this CSharpSyntaxTree syntaxTree)
        {
            return syntaxTree.AllTypes().SelectMany(t => t.Fields).ToImmutableList();
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

        public static bool IsPublic(this DeclarationWithModifiers declaration)
        {
            return declaration.Accessibility == Accessibility.Public;
        }

        public static bool IsInternal(this DeclarationWithModifiers declaration)
        {
            return declaration.Accessibility == Accessibility.Internal;
        }

        public static bool IsPrivate(this DeclarationWithModifiers declaration)
        {
            return declaration.Accessibility == Accessibility.Private;
        }

        public static bool IsStatic(this DeclarationWithModifiers declaration)
        {
            return declaration.Modifiers.Contains(Modifier.Static);
        }

        public static bool IsPartial(this DeclarationWithModifiers declaration)
        {
            return declaration.Modifiers.Contains(Modifier.Partial);
        }

        public static bool IsSealed(this DeclarationWithModifiers declaration)
        {
            return declaration.Modifiers.Contains(Modifier.Sealed);
        }

        public static bool IsAbstract(this DeclarationWithModifiers declaration)
        {
            return declaration.Modifiers.Contains(Modifier.Abstract);
        }

        public static bool IsOverride(this DeclarationWithModifiers declaration)
        {
            return declaration.Modifiers.Contains(Modifier.Override);
        }

        public static bool IsVirtual(this DeclarationWithModifiers declaration)
        {
            return declaration.Modifiers.Contains(Modifier.Virtual);
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

        private static IEnumerable<TypeDeclaration> SelfAndDescendants(TypeDeclaration type)
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
