using System.Collections.Generic;
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
        /// converted in <c>x as Foo</c>, negated in <c>!x</c>, tested in <c>x is Foo</c>, thrown in
        /// <c>throw ex</c>, switched on in <c>x switch { }</c>, copied in <c>p with { }</c>, spread in
        /// <c>..items</c>, and the body of an expression lambda. Null for any other kind.
        /// </summary>
        public Expression? Operand { get; init; }

        /// <summary>
        /// The name for an <see cref="ExpressionKind.Identifier"/>, the last name of a
        /// <see cref="ExpressionKind.MemberAccess"/>, <c>Now</c> in <c>DateTime.Now</c>, and the called
        /// method of an <see cref="ExpressionKind.Invocation"/>, <c>Send</c> in <c>client.Send()</c>.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>True when a <c>!</c> follows the expression, as in <c>null!</c> or <c>user!.Name</c> at its end.</summary>
        public bool HasNullForgiving { get; init; }

        /// <summary>
        /// The operator as written: <c>==</c>, <c>+</c>, <c>&amp;&amp;</c>, <c>??</c> of a binary expression,
        /// <c>=</c>, <c>+=</c>, <c>??=</c> of an assignment, <c>!</c>, <c>-</c>, <c>++</c>, <c>^</c> of a
        /// unary one, <c>..</c> of a range. Null for any other kind.
        /// </summary>
        public string? Operator { get; init; }

        /// <summary>The left side of a binary expression, a range or an assignment, where it is what is assigned to.</summary>
        public Expression? Left { get; init; }

        /// <summary>The right side of a binary expression, a range or an assignment, where it is the value.</summary>
        public Expression? Right { get; init; }

        /// <summary>The condition of <c>a ? b : c</c>.</summary>
        public Expression? Condition { get; init; }

        /// <summary>The value of <c>a ? b : c</c> when the condition holds, <c>b</c>.</summary>
        public Expression? WhenTrue { get; init; }

        /// <summary>The value of <c>a ? b : c</c> when the condition does not hold, <c>c</c>.</summary>
        public Expression? WhenFalse { get; init; }

        /// <summary>
        /// What a member is accessed, a method called or an element indexed on: <c>DateTime</c> in
        /// <c>DateTime.Now</c>, <c>client</c> in <c>client.Send()</c> and in <c>client?.Send()</c>,
        /// <c>items</c> in <c>items[0]</c>. Null for a plain name and for a call like <c>Go()</c>.
        /// </summary>
        public Expression? Target { get; init; }

        /// <summary>The arguments of a call, of <c>new Foo(...)</c> and of an element access, without names and <c>ref</c>; also the sizes of <c>new int[3]</c>.</summary>
        public ImmutableList<Expression> Arguments { get; init; } = ImmutableList<Expression>.Empty;

        /// <summary>
        /// The expressions in braces or brackets: the assignments of <c>new Foo { X = 1 }</c> and of
        /// <c>with { X = 1 }</c>, the elements of an array, a collection expression and a tuple, the
        /// values of an anonymous object and the results of the arms of a switch expression.
        /// </summary>
        public ImmutableList<Expression> Elements { get; init; } = ImmutableList<Expression>.Empty;

        /// <summary>The pattern of <c>x is ...</c>, taken apart; null for any other kind.</summary>
        public Pattern? Pattern { get; init; }

        /// <summary>True for <c>x++</c> and <c>x--</c>, false for <c>++x</c>.</summary>
        public bool IsPostfix { get; init; }

        /// <summary>True for an access or call through <c>?.</c> or <c>?[]</c>.</summary>
        public bool IsConditional { get; init; }

        /// <summary>
        /// The expressions directly inside this one, in source order: operands, sides, target,
        /// arguments, elements, the holes of an interpolated string and the body of an expression
        /// lambda. A lambda with a block keeps its statements to itself, see <c>Lambdas</c>.
        /// </summary>
        public ImmutableList<Expression> Children
        {
            get
            {
                IEnumerable<Expression?> children =
                [
                    Operand, Left, Right, Condition, WhenTrue, WhenFalse, Target,
                    ..Arguments, ..Elements, ..InterpolatedParts.Select(p => p.Hole),
                    ..Pattern?.DescendantsAndSelf().Select(p => p.Value) ?? []
                ];

                return children.OfType<Expression>().OrderBy(e => e.Location.SpanStart).ToImmutableList();
            }
        }

        /// <summary>This expression followed by every expression inside it, depth first, in source order.</summary>
        /// <example>
        /// <code>
        /// // x == null, which a rule may want written as x is null
        /// statement.Expressions.SelectMany(e =&gt; e.DescendantsAndSelf())
        ///          .Where(e =&gt; e.Operator == "==" &amp;&amp; e.Right?.Kind == ExpressionKind.NullLiteral)
        /// </code>
        /// </example>
        public IEnumerable<Expression> DescendantsAndSelf()
        {
            yield return this;

            foreach (var descendant in Children.SelectMany(c => c.DescendantsAndSelf()))
            {
                yield return descendant;
            }
        }

        /// <summary>Every expression inside this one, depth first, in source order.</summary>
        public IEnumerable<Expression> Descendants()
        {
            return DescendantsAndSelf().Skip(1);
        }

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

        /// <summary>
        /// A method call, with <see cref="Expression.Name"/>, <see cref="Expression.Target"/> and
        /// <see cref="Expression.Arguments"/>; argument names, <c>ref</c> and the chain are in the
        /// member's <c>Invocations</c>.
        /// </summary>
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

        Base,

        /// <summary><c>1..^1</c>, <c>..3</c> and <c>..</c>; see <see cref="Expression.Left"/> and <see cref="Expression.Right"/>.</summary>
        Range,

        /// <summary>The <c>..items</c> in a collection expression; see <see cref="Expression.Operand"/>.</summary>
        Spread,

        /// <summary>A LINQ query, <c>from x in items select x</c>.</summary>
        Query
    }
}
