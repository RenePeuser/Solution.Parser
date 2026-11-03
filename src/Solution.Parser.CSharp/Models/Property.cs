using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Type} {Name}")]
    public record Property : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        /// <summary>The type taken apart, see <see cref="CSharp.TypeReference"/>; <see cref="Type"/> is the same as text.</summary>
        public TypeReference TypeReference { get; init; } = TypeReference.None;

        public PropertyAccessors Accessors { get; init; } = PropertyAccessors.None;

        /// <summary>The value after <c>=</c>, for example in <c>public int X { get; } = 3;</c>.</summary>
        public string? Initializer { get; init; }

        /// <summary>The initializer classified, see <see cref="Expression"/>; <see cref="Initializer"/> is the same as text.</summary>
        public Expression? InitializerExpression { get; init; }

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

        /// <summary>
        /// What the member extends: the <c>this</c> parameter of a classic extension method,
        /// <c>Shout(this string value)</c>, or the receiver of the C# 14 extension block the member is
        /// declared in, <c>extension(string value)</c>. Null for a member that extends nothing. A static
        /// member of a block, which extends the type rather than an instance, has a receiver without a
        /// name when the block gives none.
        /// </summary>
        public Parameter? ExtensionReceiver { get; init; }

        /// <summary>True for an extension member, written either way; see <see cref="ExtensionReceiver"/>.</summary>
        public bool IsExtension => ExtensionReceiver is not null;

        /// <summary>True for a member of a C# 14 <c>extension(...) { }</c> block, false for a classic <c>this</c> extension method.</summary>
        public bool IsInExtensionBlock { get; init; }
    }

    /// <summary>The accessors of a property or indexer. A missing accessor is null.</summary>
    public record PropertyAccessors(Accessor? Get, Accessor? Set, Accessor? Init)
    {
        public static PropertyAccessors None { get; } = new(null, null, null);

        /// <summary>The accessors that are there, in the order get, set, init.</summary>
        public IEnumerable<Accessor> All => new[] { Get, Set, Init }.OfType<Accessor>();
    }

    /// <summary>
    /// One accessor, <c>get</c>, <c>set</c> or <c>init</c> of a property or indexer and <c>add</c> or
    /// <c>remove</c> of an event. It carries its body like a method does, so the calls, variables and
    /// statements of <c>get { return _cache ??= Load(); }</c> are found by the same rules.
    /// <see cref="DeclarationWithModifiers.Accessibility"/> carries the accessor's own modifier, as in
    /// <c>{ get; private set; }</c>, and is <see cref="Accessibility.NotApplicable"/> when it has none.
    /// </summary>
    /// <remarks>
    /// <see cref="DeclarationBase.Name"/> is the keyword, <c>get</c>, and
    /// <see cref="DeclarationBase.FullyQualifiedName"/> that of the property followed by it,
    /// <c>Shop.Cart.Total.get</c>. An expression bodied property, <c>int X =&gt; 3;</c>, has a getter
    /// whose body is that expression.
    /// </remarks>
    [DebuggerDisplay("{Kind}")]
    public record Accessor : MemberWithBody
    {
        public required AccessorKind Kind { get; init; }

        /// <summary>True for a bare <c>get;</c>, whose body the compiler supplies.</summary>
        public bool IsAutoImplemented { get; init; }
    }

    public enum AccessorKind
    {
        Get,

        Set,

        Init,

        Add,

        Remove
    }
}
