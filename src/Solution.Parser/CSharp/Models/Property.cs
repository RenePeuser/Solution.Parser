using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Type} {Name}")]
    public record Property : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        public PropertyAccessors Accessors { get; init; } = PropertyAccessors.None;

        /// <summary>The value after <c>=</c>, for example in <c>public int X { get; } = 3;</c>.</summary>
        public Initializer? Initializer { get; init; }

        /// <summary>True when every accessor is a semicolon, so the compiler supplies the backing field.</summary>
        public bool IsAutoProperty { get; init; }

        /// <summary>True for <c>public int X =&gt; 3;</c>.</summary>
        public bool IsExpressionBodied { get; init; }

        /// <summary>The interface in <c>int IFoo.X { get; }</c>, otherwise null.</summary>
        public string? ExplicitInterfaceSpecifier { get; init; }

        /// <summary>
        /// True when the property has no <c>set</c> accessor. An <c>init</c> accessor does not count as
        /// one, so <c>{ get; init; }</c> is read only, which is what an immutability rule asks about.
        /// </summary>
        public bool IsReadOnly => Accessors.Set is null;

        public bool IsRequired => Modifiers.Contains(Modifier.Required);

        /// <summary>
        /// True when the property type itself carries a nullable annotation. A type argument that is
        /// nullable does not make the property nullable, so <c>List&lt;int?&gt;</c> is not nullable.
        /// </summary>
        public bool IsNullable { get; init; }

        public bool HasGetter => Accessors.Get is not null;

        public bool HasSetter => Accessors.Set is not null;

        public bool IsInitOnly => Accessors.Init is not null;

        public bool IsExplicitInterfaceImplementation => ExplicitInterfaceSpecifier is not null;
    }

    /// <summary>The accessors of a property or indexer. A missing accessor is null.</summary>
    public record PropertyAccessors(Accessor? Get, Accessor? Set, Accessor? Init)
    {
        public static PropertyAccessors None { get; } = new(null, null, null);
    }

    /// <summary>
    /// One accessor. <see cref="Accessibility"/> carries the accessor's own modifier, as in
    /// <c>{ get; private set; }</c>.
    /// </summary>
    [DebuggerDisplay("{Kind}")]
    public record Accessor(AccessorKind Kind,
                           Accessibility Accessibility,
                           bool IsAutoImplemented,
                           bool IsExpressionBodied,
                           string Body,
                           CodeLocation Location);

    public enum AccessorKind
    {
        Get,

        Set,

        Init
    }
}
