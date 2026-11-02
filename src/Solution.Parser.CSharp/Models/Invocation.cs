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
    [DebuggerDisplay("{SourceCode}")]
    public record Invocation : DeclarationBase
    {
        /// <summary>The expression the method is called on, <c>Client</c> in <c>Client.Go()</c>; null for a plain <c>Go()</c>.</summary>
        public string? Target { get; init; }

        public ImmutableList<InvocationArgument> Arguments { get; init; } = ImmutableList<InvocationArgument>.Empty;

        /// <summary>The explicit type arguments, <c>int</c> in <c>Go&lt;int&gt;()</c>.</summary>
        public ImmutableList<string> TypeArguments { get; init; } = ImmutableList<string>.Empty;

        /// <summary>True for a call through <c>?.</c>.</summary>
        public bool IsConditional { get; init; }

        /// <summary>
        /// The call whose result this call is made on, <c>MapGet(...)</c> for <c>WithName</c> in
        /// <c>app.MapGet(...).WithName("x")</c>; null when the call is not made on another call.
        /// It is the same instance the member's <c>Invocations</c> holds.
        /// </summary>
        public Invocation? Receiver { get; init; }

        /// <summary>The chain up to and including this call, first call first: <c>MapGet</c>, <c>WithName</c>.</summary>
        public ImmutableList<Invocation> ChainUpToHere()
        {
            var calls = new System.Collections.Generic.List<Invocation>();

            for (var call = this; call is not null; call = call.Receiver)
            {
                calls.Add(call);
            }

            calls.Reverse();

            return calls.ToImmutableList();
        }
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

        /// <summary>
        /// The argument classified, see <see cref="CSharp.Expression"/>: a string literal with its value,
        /// a lambda, a <c>nameof</c>, a <c>typeof</c>, ... <see cref="Expression"/> is the same as text.
        /// </summary>
        public Expression? ValueExpression { get; init; }

        /// <summary>What kind of expression is passed; <see cref="ExpressionKind.Other"/> when unknown.</summary>
        public ExpressionKind Kind => ValueExpression?.Kind ?? ExpressionKind.Other;

        /// <summary>The value of a literal or <c>nameof</c>, see <see cref="CSharp.Expression.LiteralValue"/>.</summary>
        public object? LiteralValue => ValueExpression?.LiteralValue;

        /// <summary>The text of a string literal or <c>nameof</c>, without quotes and escapes; null for anything else.</summary>
        public string? StringValue => ValueExpression?.StringValue;

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
