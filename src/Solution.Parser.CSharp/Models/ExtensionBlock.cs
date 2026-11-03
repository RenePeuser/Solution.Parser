using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A C# 14 extension block, <c>extension(string value) { ... }</c>, inside a static class. It is
    /// no type of its own: its members extend <see cref="Receiver"/>, and the compiler emits them on the
    /// static class. So <c>tree.Methods</c>, <c>tree.Properties</c> and <c>tree.MembersWithBody</c> include
    /// them, while <see cref="TypeDeclaration.Methods"/> keeps to what the class declares itself.
    /// </summary>
    /// <example>
    /// <code>
    /// from type in tree.AllTypes()
    /// from block in type.ExtensionBlocks
    /// where block.Receiver.TypeReference.Is("HttpClient")
    /// from method in block.Methods
    /// select method.Name;
    /// </code>
    /// </example>
    [DebuggerDisplay("extension({Receiver.Type} {Receiver.Name})")]
    public sealed record ExtensionBlock : CodeElement
    {
        /// <summary>What is extended, <c>string value</c>; its name is empty for <c>extension(string)</c>, whose members are all static.</summary>
        public required Parameter Receiver { get; init; }

        /// <summary>The type parameters of <c>extension&lt;T&gt;(IEnumerable&lt;T&gt; source)</c>.</summary>
        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        public ImmutableList<Method> Methods { get; init; } = ImmutableList<Method>.Empty;

        public ImmutableList<Property> Properties { get; init; } = ImmutableList<Property>.Empty;

        public ImmutableList<Operator> Operators { get; init; } = ImmutableList<Operator>.Empty;

        /// <summary>True when the receiver has a name, so the members can be called on an instance.</summary>
        public bool HasReceiverName => Receiver.Name.Length > 0;
    }
}
