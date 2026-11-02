using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A <c>new Foo(...)</c> or a target typed <c>new(...)</c> in a body.
    /// </summary>
    /// <example>
    /// <code>
    /// from method in tree.AllMethods()
    /// from creation in method.ObjectCreations
    /// where creation.TypeReference.Is("HttpClient")
    /// select $"{creation.Location}: use IHttpClientFactory instead of new HttpClient()";
    /// </code>
    /// </example>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record ObjectCreation : CodeElement
    {
        /// <summary>
        /// The created type. For a target typed <c>new()</c> it is taken from the declaration it
        /// initialises, as in <c>List&lt;int&gt; x = new();</c> or a field or property of that type;
        /// <see cref="CSharp.TypeReference.None"/> when the syntax does not tell, as for an argument.
        /// </summary>
        public required TypeReference TypeReference { get; init; }

        /// <summary>The created type's name, <c>List</c> for <c>new List&lt;int&gt;()</c>; empty when unknown.</summary>
        public string TypeName => TypeReference.Name;

        /// <summary>True for <c>new()</c>, where the type comes from the context.</summary>
        public bool IsTargetTyped { get; init; }

        public ImmutableList<InvocationArgument> Arguments { get; init; } = ImmutableList<InvocationArgument>.Empty;

        /// <summary>The object or collection initializer with its braces, <c>{ Name = "x" }</c>; null when there is none.</summary>
        public string? Initializer { get; init; }

        /// <summary>The <c>Name = value</c> assignments of an object initializer, in source order.</summary>
        public ImmutableList<InitializedMember> InitializedMembers { get; init; } = ImmutableList<InitializedMember>.Empty;
    }

    /// <summary>One <c>Name = value</c> of an object initializer.</summary>
    [DebuggerDisplay("{Name} = {Value.SourceCode}")]
    public sealed record InitializedMember(string Name, Expression Value);
}
