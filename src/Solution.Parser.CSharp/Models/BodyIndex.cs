using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The lists of one body, sorted by position, so a statement can cut out what lies inside it on
    /// first use instead of every statement collecting its own copy while the body is built.
    /// </summary>
    /// <remarks>
    /// A body has hundreds of statements and each sits inside a few others. Handing every statement its
    /// own lists up front would cost memory for each level of nesting, while a rule only ever asks a
    /// few of them. Binary search over the shared lists makes asking cheap and not asking free.
    /// </remarks>
    internal sealed class BodyIndex
    {
        private Statement[] _statements = [];
        private LocalVariable[] _localVariables = [];
        private ObjectCreation[] _objectCreations = [];
        private StringLiteral[] _stringLiterals = [];
        private MemberAccess[] _memberAccesses = [];
        private Lambda[] _lambdas = [];
        private SwitchExpression[] _switchExpressions = [];
        private Expression[] _awaits = [];

        /// <summary>Fills the index once the whole body is built; until then every statement reads it as empty.</summary>
        internal void Complete(MemberBody body)
        {
            _statements = Sorted(body.AllStatements);
            _localVariables = Sorted(body.LocalVariables);
            _objectCreations = Sorted(body.ObjectCreations);
            _stringLiterals = Sorted(body.StringLiterals);
            _memberAccesses = Sorted(body.MemberAccesses);
            _lambdas = Sorted(body.Lambdas);
            _switchExpressions = Sorted(body.SwitchExpressions);
            _awaits = Sorted(body.Awaits);
        }

        /// <summary>
        /// The statements inside <paramref name="statement"/>, itself excluded; null when it is not the very
        /// instance this index was built with, a copy made with <c>with</c> for one, whose children may differ.
        /// </summary>
        internal ImmutableList<Statement>? StatementsInside(Statement statement)
        {
            var (start, end) = SpanOf(statement);
            var candidates = SpanSearch.InSpan(_statements, s => s.Location.SpanStart, start, start + 1);

            return candidates.Any(c => ReferenceEquals(c, statement))
                       ? SpanSearch.InSpan(_statements, s => s.Location.SpanStart, start + 1, end)
                       : null;
        }

        internal ImmutableList<LocalVariable> LocalVariablesIn(Statement statement) => In(_localVariables, statement);

        internal ImmutableList<ObjectCreation> ObjectCreationsIn(Statement statement) => In(_objectCreations, statement);

        internal ImmutableList<StringLiteral> StringLiteralsIn(Statement statement) => In(_stringLiterals, statement);

        internal ImmutableList<MemberAccess> MemberAccessesIn(Statement statement) => In(_memberAccesses, statement);

        internal ImmutableList<Lambda> LambdasIn(Statement statement) => In(_lambdas, statement);

        internal ImmutableList<SwitchExpression> SwitchExpressionsIn(Statement statement) => In(_switchExpressions, statement);

        internal ImmutableList<Expression> AwaitsIn(Statement statement) => In(_awaits, statement);

        private static ImmutableList<T> In<T>(T[] sorted, Statement statement)
            where T : CodeElement
        {
            var (start, end) = SpanOf(statement);

            return SpanSearch.InSpan(sorted, e => e.Location.SpanStart, start, end);
        }

        private static (int Start, int End) SpanOf(Statement statement)
        {
            return (statement.Location.SpanStart, statement.Location.SpanStart + statement.Location.SpanLength);
        }

        private static T[] Sorted<T>(IEnumerable<T> elements)
            where T : CodeElement
        {
            // The body is walked in source order, so the lists nearly always come sorted; checking costs
            // one pass, sorting what is sorted already cost a measurable share of parsing.
            var array = elements.ToArray();

            for (var i = 1; i < array.Length; i++)
            {
                if (array[i - 1].Location.SpanStart > array[i].Location.SpanStart)
                {
                    return array.OrderBy(e => e.Location.SpanStart).ToArray();
                }
            }

            return array;
        }
    }

    /// <summary>
    /// A field that takes no part in record equality, for what a record caches or how it was built.
    /// Records compare every field, so a cache filled on one instance and not yet on the other would
    /// otherwise make two equal elements unequal.
    /// </summary>
    internal struct Unequated<T> : IEquatable<Unequated<T>>
        where T : class
    {
        internal T? Value;

        public readonly bool Equals(Unequated<T> other) => true;

        public override readonly bool Equals(object? obj) => obj is Unequated<T>;

        public override readonly int GetHashCode() => 0;
    }

    /// <summary>A cached value together with the instance it was computed for.</summary>
    internal sealed class Cached<T>(object owner, T value)
        where T : class
    {
        internal object Owner { get; } = owner;

        internal T Value { get; } = value;
    }

    internal static class Cache
    {
        /// <summary>
        /// The value cached for <paramref name="owner"/>, computed on first use. A record copied with
        /// <c>with</c> copies its fields, the cache included, so the cache remembers its owner and a copy
        /// computes its own. Owner and value are swapped in as one reference, so a reader on another
        /// thread never sees the value of one with the owner of the other.
        /// </summary>
        internal static T For<TOwner, T>(ref Unequated<Cached<T>> slot, TOwner owner, Func<TOwner, T> create)
            where TOwner : class
            where T : class
        {
            if (slot.Value is { } cached && ReferenceEquals(cached.Owner, owner))
            {
                return cached.Value;
            }

            var value = create(owner);
            slot.Value = new Cached<T>(owner, value);

            return value;
        }
    }
}
