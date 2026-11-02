using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// An expression as written, classified: <c>"api/users"</c> is a <see cref="ExpressionKind.StringLiteral"/>
    /// with the value <c>api/users</c>, <c>typeof(Foo)</c> a <see cref="ExpressionKind.TypeOf"/> of
    /// <c>Foo</c>, <c>x =&gt; x.Id</c> a <see cref="ExpressionKind.Lambda"/>. It saves a rule from
    /// guessing the meaning from the text with <c>StartsWith("\"")</c> or <c>Contains("typeof")</c>.
    /// </summary>
    /// <remarks>
    /// Parentheses and a trailing <c>!</c> do not change the kind: <c>(null!)</c> is a
    /// <see cref="ExpressionKind.NullLiteral"/> with <see cref="HasNullForgiving"/> set. A plain name
    /// stays an <see cref="ExpressionKind.Identifier"/>, even when it is a method group, because only
    /// symbols can tell the two apart.
    /// </remarks>
    [DebuggerDisplay("{Kind} {SourceCode}")]
    public sealed record Expression : CodeElement
    {
        public required ExpressionKind Kind { get; init; }

        /// <summary>
        /// The value the compiler sees in a literal: the text of a string without quotes and escapes
        /// (also for verbatim, raw and <c>u8</c> strings), the number, the bool, the char. For a
        /// <c>nameof(Foo.Bar)</c> it is <c>Bar</c>. Null for anything else, and for <c>null</c> itself.
        /// </summary>
        public object? LiteralValue { get; init; }

        /// <summary>
        /// The parts of an interpolated string in source order; empty for any other kind. The text of
        /// <c>$"api/{version}/users"</c> is <c>api/</c>, the hole <c>version</c> and the text <c>/users</c>.
        /// </summary>
        public ImmutableList<InterpolatedStringPart> InterpolatedParts { get; init; } = ImmutableList<InterpolatedStringPart>.Empty;

        /// <summary>
        /// The type an expression names: <c>Foo</c> in <c>typeof(Foo)</c>, <c>new Foo()</c>,
        /// <c>default(Foo)</c>, <c>(Foo)x</c> and <c>x as Foo</c>. Null for any other kind and for a
        /// target typed <c>new()</c>.
        /// </summary>
        public TypeReference? TypeReference { get; init; }

        /// <summary>
        /// The expression inside: what is awaited in <c>await x</c>, cast in <c>(Foo)x</c> or
        /// converted in <c>x as Foo</c>, negated in <c>!x</c>. Null for any other kind.
        /// </summary>
        public Expression? Operand { get; init; }

        /// <summary>The name for an <see cref="ExpressionKind.Identifier"/> and the last name of a <see cref="ExpressionKind.MemberAccess"/>, <c>Now</c> in <c>DateTime.Now</c>.</summary>
        public string? Name { get; init; }

        /// <summary>True when a <c>!</c> follows the expression, as in <c>null!</c> or <c>user!.Name</c> at its end.</summary>
        public bool HasNullForgiving { get; init; }

        public bool IsLiteral => Kind is ExpressionKind.StringLiteral or ExpressionKind.NumericLiteral or ExpressionKind.BooleanLiteral
                                     or ExpressionKind.CharLiteral or ExpressionKind.NullLiteral or ExpressionKind.DefaultLiteral;

        /// <summary>The text of a string literal or a <c>nameof</c>; null for anything else, also for an interpolated string.</summary>
        public string? StringValue => LiteralValue as string;

        /// <summary>
        /// The text of an interpolated string with each hole written as <paramref name="holePlaceholder"/>,
        /// <c>api/{}/users</c> for <c>$"api/{version}/users"</c>. For a plain string literal its value.
        /// Null for anything else.
        /// </summary>
        public string? TextWithHoles(string holePlaceholder = "{}")
        {
            if (Kind == ExpressionKind.StringLiteral)
            {
                return StringValue;
            }

            return Kind == ExpressionKind.InterpolatedString
                       ? string.Concat(InterpolatedParts.Select(p => p.IsHole ? holePlaceholder : p.Text))
                       : null;
        }
    }

    /// <summary>One part of an interpolated string: either text or a hole, <c>{version}</c>.</summary>
    [DebuggerDisplay("{Text}")]
    public sealed record InterpolatedStringPart
    {
        /// <summary>The text with <c>{{</c> and <c>}}</c> already turned into braces, or the expression of a hole as written.</summary>
        public required string Text { get; init; }

        /// <summary>The expression of a hole, null for text.</summary>
        public Expression? Hole { get; init; }

        /// <summary>The format after the colon, <c>yyyy-MM-dd</c> in <c>{date:yyyy-MM-dd}</c>.</summary>
        public string? Format { get; init; }

        public bool IsHole => Hole is not null;
    }

    public enum ExpressionKind
    {
        /// <summary>Anything without a kind of its own, see the source.</summary>
        Other,

        StringLiteral,

        /// <summary><c>$"..."</c>, see <see cref="Expression.InterpolatedParts"/>.</summary>
        InterpolatedString,

        NumericLiteral,

        BooleanLiteral,

        CharLiteral,

        NullLiteral,

        /// <summary>A plain <c>default</c>, without a type.</summary>
        DefaultLiteral,

        /// <summary>A plain name: a local, a parameter, a field, a property, a type or a method group.</summary>
        Identifier,

        /// <summary><c>a.b</c>, including <c>a?.b</c>.</summary>
        MemberAccess,

        /// <summary>A method call; the details are in the member's <c>Invocations</c>.</summary>
        Invocation,

        /// <summary><c>new Foo()</c> and the target typed <c>new()</c>.</summary>
        ObjectCreation,

        /// <summary><c>new[] { }</c>, <c>new int[3]</c> and <c>stackalloc</c>.</summary>
        ArrayCreation,

        /// <summary><c>[1, 2, 3]</c> and the empty <c>[]</c>.</summary>
        CollectionExpression,

        /// <summary><c>new { Id = 1 }</c>.</summary>
        AnonymousObject,

        /// <summary><c>x =&gt; ...</c>, <c>(a, b) =&gt; ...</c> and <c>delegate { ... }</c>.</summary>
        Lambda,

        TypeOf,

        NameOf,

        /// <summary><c>default(Foo)</c>; a plain <c>default</c> is a <see cref="DefaultLiteral"/>.</summary>
        Default,

        /// <summary><c>(Foo)x</c>.</summary>
        Cast,

        /// <summary><c>x as Foo</c>.</summary>
        As,

        /// <summary><c>x is Foo</c> and <c>x is { } y</c>.</summary>
        Is,

        Await,

        /// <summary><c>a ? b : c</c>.</summary>
        Conditional,

        /// <summary><c>a + b</c>, <c>a &amp;&amp; b</c>, <c>a ?? b</c> and the like.</summary>
        Binary,

        /// <summary><c>!a</c>, <c>-a</c>, <c>++a</c>, <c>a++</c> and the like.</summary>
        Unary,

        /// <summary><c>a = b</c>, <c>a += b</c>, <c>a ??= b</c>.</summary>
        Assignment,

        /// <summary><c>x switch { ... }</c>, see the member's <c>SwitchExpressions</c>.</summary>
        Switch,

        /// <summary><c>throw new Foo()</c> used as an expression, as in <c>x ?? throw ...</c>.</summary>
        Throw,

        /// <summary><c>a[0]</c>.</summary>
        ElementAccess,

        /// <summary><c>(a, b)</c>.</summary>
        Tuple,

        /// <summary><c>record with { X = 1 }</c>.</summary>
        With,

        This,

        Base
    }
}
