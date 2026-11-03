using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A pattern as written after <c>is</c>, <c>case</c> or in a switch arm, taken apart:
    /// <c>Order { Status: "open", Total: &gt; 100 } o</c> is a <see cref="PatternKind.Recursive"/> pattern
    /// of type <c>Order</c> with two property patterns and the designation <c>o</c>. It saves a rule
    /// from reading the pattern as text.
    /// </summary>
    /// <example>
    /// <code>
    /// // x is null, x is not null, case null:
    /// method.Expressions().Where(e =&gt; e.Pattern?.IsNull == true)
    /// </code>
    /// </example>
    [DebuggerDisplay("{Kind} {SourceCode}")]
    public sealed record Pattern : CodeElement
    {
        public required PatternKind Kind { get; init; }

        /// <summary>
        /// The type the pattern tests for: <c>Foo</c> in <c>is Foo</c>, <c>is Foo f</c> and
        /// <c>is Foo { }</c>. Null when the pattern names no type.
        /// </summary>
        public TypeReference? TypeReference { get; init; }

        /// <summary>The variable the pattern declares, <c>f</c> in <c>is Foo f</c>; null when it declares none.</summary>
        public string? Designation { get; init; }

        /// <summary>The value of a constant pattern, <c>null</c> in <c>is null</c>, or what a relational pattern compares with, <c>100</c> in <c>&gt; 100</c>.</summary>
        public Expression? Value { get; init; }

        /// <summary>The operator of a relational pattern, <c>&gt;</c>, <c>&lt;=</c> and the like; null for any other kind.</summary>
        public string? Operator { get; init; }

        /// <summary>What a <c>not</c> or a slice pattern applies to.</summary>
        public Pattern? Operand { get; init; }

        /// <summary>The left side of an <c>and</c> or <c>or</c> pattern.</summary>
        public Pattern? Left { get; init; }

        /// <summary>The right side of an <c>and</c> or <c>or</c> pattern.</summary>
        public Pattern? Right { get; init; }

        /// <summary>The patterns in the parentheses of a positional pattern, <c>Point(0, var y)</c>.</summary>
        public ImmutableList<Pattern> Positional { get; init; } = ImmutableList<Pattern>.Empty;

        /// <summary>The property patterns in the braces, <c>Status: "open"</c> in <c>{ Status: "open" }</c>.</summary>
        public ImmutableList<PropertyPattern> Properties { get; init; } = ImmutableList<PropertyPattern>.Empty;

        /// <summary>The elements of a list pattern, <c>[1, .., var last]</c>.</summary>
        public ImmutableList<Pattern> Elements { get; init; } = ImmutableList<Pattern>.Empty;

        /// <summary>True for <c>null</c>, as in <c>x is null</c> and <c>case null:</c>.</summary>
        public bool IsNull => Kind == PatternKind.Constant && Value?.Kind == ExpressionKind.NullLiteral;

        /// <summary>
        /// True for a pattern that matches any value but null: <c>not null</c> and the empty
        /// property pattern <c>{ }</c>, also with a designation as in <c>{ } user</c>.
        /// </summary>
        public bool IsNotNull => (Kind == PatternKind.Not && Operand?.IsNull == true) ||
                                 (Kind == PatternKind.Recursive && TypeReference is null && Positional.IsEmpty && Properties.IsEmpty && IsPropertyClause);

        /// <summary>True when a recursive pattern has braces, even empty ones.</summary>
        public bool IsPropertyClause { get; init; }

        /// <summary>The patterns directly inside this one, in source order.</summary>
        public ImmutableList<Pattern> Children
        {
            get
            {
                IEnumerable<Pattern?> children = [Operand, Left, Right, ..Positional, ..Properties.Select(p => p.Pattern), ..Elements];

                return children.OfType<Pattern>().OrderBy(p => p.Location.SpanStart).ToImmutableList();
            }
        }

        /// <summary>This pattern followed by every pattern inside it, depth first.</summary>
        public IEnumerable<Pattern> DescendantsAndSelf()
        {
            yield return this;

            foreach (var descendant in Children.SelectMany(c => c.DescendantsAndSelf()))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>One <c>Name: pattern</c> in the braces of a property pattern, <c>Address.City: "Berlin"</c> included.</summary>
    [DebuggerDisplay("{Name}: {Pattern.SourceCode}")]
    public sealed record PropertyPattern
    {
        /// <summary>The property as written, <c>Status</c> or <c>Address.City</c>.</summary>
        public required string Name { get; init; }

        public required Pattern Pattern { get; init; }
    }

    public enum PatternKind
    {
        /// <summary>Anything without a kind of its own, see the source.</summary>
        Other,

        /// <summary><c>null</c>, <c>42</c>, <c>"admin"</c>, <c>Color.Red</c>; see <see cref="Pattern.Value"/>.</summary>
        Constant,

        /// <summary><c>Foo</c> on its own, as in <c>x is Foo</c>.</summary>
        Type,

        /// <summary><c>Foo f</c>, a type with a variable.</summary>
        Declaration,

        /// <summary><c>var x</c>.</summary>
        Var,

        /// <summary><c>_</c>.</summary>
        Discard,

        /// <summary><c>&gt; 100</c>, <c>&lt;= 0</c>; see <see cref="Pattern.Operator"/>.</summary>
        Relational,

        /// <summary><c>not ...</c>; see <see cref="Pattern.Operand"/>.</summary>
        Not,

        /// <summary><c>... and ...</c>.</summary>
        And,

        /// <summary><c>... or ...</c>.</summary>
        Or,

        /// <summary><c>Foo { X: 1 }</c>, <c>Point(1, 2)</c>, <c>{ }</c>; see <see cref="Pattern.Properties"/> and <see cref="Pattern.Positional"/>.</summary>
        Recursive,

        /// <summary><c>[1, 2, ..]</c>; see <see cref="Pattern.Elements"/>.</summary>
        List,

        /// <summary>The <c>..</c> in a list pattern.</summary>
        Slice
    }
}
