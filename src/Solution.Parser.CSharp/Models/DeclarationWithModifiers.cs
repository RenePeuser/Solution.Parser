using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A declaration that can carry modifiers and attributes. Every state a rule asks about is a
    /// property: <c>method.IsStatic</c>, <c>type.IsSealed</c>, <c>field.IsPublic</c>.
    /// </summary>
    /// <remarks>
    /// The accessibility checks read <see cref="Accessibility"/>, which includes the language default:
    /// a class member without a modifier is <see cref="IsPrivate"/>, an interface member <see cref="IsPublic"/>.
    /// The modifier checks read <see cref="Modifiers"/> as written: a body-less interface member is
    /// abstract to the compiler but not <see cref="IsAbstract"/> here.
    /// </remarks>
    [DebuggerDisplay("{Name}")]
    public abstract record DeclarationWithModifiers : DeclarationBase
    {
        public ImmutableList<Modifier> Modifiers { get; init; } = ImmutableList<Modifier>.Empty;

        public ImmutableList<Attribute> Attributes { get; init; } = ImmutableList<Attribute>.Empty;

        /// <summary>
        /// The effective accessibility, including the language default when no modifier is written.
        /// </summary>
        public Accessibility Accessibility { get; init; } = Accessibility.NotApplicable;

        public DocumentationComment Documentation { get; init; } = DocumentationComment.None;

        /// <summary><c>public</c>, written or by default as on an interface member.</summary>
        public bool IsPublic => Accessibility == Accessibility.Public;

        /// <summary><c>internal</c>, written or by default as on a type in a namespace.</summary>
        public bool IsInternal => Accessibility == Accessibility.Internal;

        /// <summary><c>private</c>, written or by default as on a class member.</summary>
        public bool IsPrivate => Accessibility == Accessibility.Private;

        /// <summary>Exactly <c>protected</c>; <c>protected internal</c> and <c>private protected</c> have their own check.</summary>
        public bool IsProtected => Accessibility == Accessibility.Protected;

        /// <summary><c>protected internal</c>: visible to derived types or to the same assembly.</summary>
        public bool IsProtectedInternal => Accessibility == Accessibility.ProtectedOrInternal;

        /// <summary><c>private protected</c>: visible to derived types in the same assembly.</summary>
        public bool IsPrivateProtected => Accessibility == Accessibility.ProtectedAndInternal;

        public bool IsStatic => Modifiers.Contains(Modifier.Static);

        public bool IsAbstract => Modifiers.Contains(Modifier.Abstract);

        public bool IsVirtual => Modifiers.Contains(Modifier.Virtual);

        public bool IsOverride => Modifiers.Contains(Modifier.Override);

        public bool IsSealed => Modifiers.Contains(Modifier.Sealed);

        /// <summary>This part of the declaration is written <c>partial</c>.</summary>
        public bool IsPartial => Modifiers.Contains(Modifier.Partial);

        /// <summary>Hides an inherited member with <c>new</c>.</summary>
        public bool IsNew => Modifiers.Contains(Modifier.New);

        public bool IsExtern => Modifiers.Contains(Modifier.Extern);

        public bool IsUnsafe => Modifiers.Contains(Modifier.Unsafe);
    }
}
