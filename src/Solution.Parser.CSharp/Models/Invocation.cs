using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A method call as written in a body, as in <c>Client.AssertPostAsync("..", "..", true)</c>.
    /// </summary>
    /// <remarks>
    /// This is what the syntax tells: the name, the receiver and the arguments in source order. Which
    /// method is meant and which parameter an argument binds to needs symbols, see the semantic
    /// package.
    /// </remarks>
    [DebuggerDisplay("{SyntaxTree}")]
    public record Invocation : DeclarationBase
    {
        /// <summary>The expression the method is called on, <c>Client</c> in <c>Client.Go()</c>; null for a plain <c>Go()</c>.</summary>
        public string? Target { get; init; }

        public ImmutableList<InvocationArgument> Arguments { get; init; } = ImmutableList<InvocationArgument>.Empty;

        /// <summary>The explicit type arguments, <c>int</c> in <c>Go&lt;int&gt;()</c>.</summary>
        public ImmutableList<string> TypeArguments { get; init; } = ImmutableList<string>.Empty;

        /// <summary>True for a call through <c>?.</c>.</summary>
        public bool IsConditional { get; init; }
    }

    /// <summary>One argument of an <see cref="Invocation"/>.</summary>
    [DebuggerDisplay("{Expression}")]
    public record InvocationArgument
    {
        /// <summary>The position in the argument list, zero based.</summary>
        public required int Ordinal { get; init; }

        /// <summary>The name of a named argument, <c>writeResponse</c> in <c>writeResponse: true</c>; null when passed by position.</summary>
        public string? Name { get; init; }

        /// <summary>The expression as written, without name and without <c>ref</c>, <c>out</c> or <c>in</c>.</summary>
        public required string Expression { get; init; }

        public ArgumentRefKind RefKind { get; init; }

        public CodeLocation Location { get; init; } = CodeLocation.None;

        public bool IsNamed => Name is not null;
    }

    public enum ArgumentRefKind
    {
        None,
        Ref,
        Out,
        In
    }

    public static class InvocationListExtensions
    {
        public static ImmutableList<Invocation> Named(this ImmutableList<Invocation> invocations, string name)
        {
            return invocations.Where(i => i.Name.Equals(name, StringComparison.Ordinal)).ToImmutableList();
        }

        /// <summary>
        /// The argument given with <c>name:</c>. The syntax alone cannot tell which parameter a
        /// positional argument binds to; with symbols loaded, <c>BoundArgument</c> can.
        /// </summary>
        public static InvocationArgument? NamedArgument(this Invocation invocation, string name)
        {
            return invocation.Arguments.FirstOrDefault(a => a.Name == name);
        }
    }
}
