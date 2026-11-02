using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Calls made one on the result of the other, as written in a fluent API:
    /// <c>app.MapGet("/users", handler).WithName("GetUsers").RequireAuthorization()</c> is a chain of
    /// <c>MapGet</c>, <c>WithName</c> and <c>RequireAuthorization</c> on <c>app</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// from chain in method.InvocationChains
    /// where chain.Contains("MapGet") &amp;&amp; !chain.Contains("RequireAuthorization")
    /// select $"{chain.Location}: endpoint {chain.Calls[0].Arguments[0].StringValue} is not protected";
    /// </code>
    /// </example>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record InvocationChain : CodeElement
    {
        /// <summary>The calls, first call first. A single call that is no part of a chain is a chain of one.</summary>
        public required ImmutableList<Invocation> Calls { get; init; }

        /// <summary>What the first call is made on, <c>app</c>; null for a plain <c>Go()</c>.</summary>
        public string? Root => Calls[0].Target;

        public Invocation First => Calls[0];

        public Invocation Last => Calls[^1];

        /// <summary>The names of the calls in order, <c>MapGet</c>, <c>WithName</c>, <c>RequireAuthorization</c>.</summary>
        public ImmutableList<string> Names => Calls.Select(c => c.Name).ToImmutableList();

        public bool Contains(string name)
        {
            return Calls.Any(c => c.Name.Equals(name, StringComparison.Ordinal));
        }

        /// <summary>The calls after the first one named <paramref name="name"/>; empty when there is none.</summary>
        public ImmutableList<Invocation> After(string name)
        {
            var index = Calls.FindIndex(c => c.Name.Equals(name, StringComparison.Ordinal));

            return index < 0 ? ImmutableList<Invocation>.Empty : Calls.RemoveRange(0, index + 1);
        }

        /// <summary>
        /// The chains of the given calls: one per call that no other call is made on, so a call ends up in
        /// exactly one chain.
        /// </summary>
        internal static ImmutableList<InvocationChain> Of(IReadOnlyCollection<Invocation> invocations)
        {
            var receivers = new HashSet<Invocation>(invocations.Select(i => i.Receiver).OfType<Invocation>(), ReferenceEqualityComparer.Instance);

            return invocations.Where(i => !receivers.Contains(i))
                              .Select(last => new InvocationChain
                              {
                                  Calls = last.ChainUpToHere(),
                                  SourceCode = last.SourceCode,
                                  FilePath = last.FilePath,
                                  Location = last.Location
                              })
                              .ToImmutableList();
        }
    }
}
