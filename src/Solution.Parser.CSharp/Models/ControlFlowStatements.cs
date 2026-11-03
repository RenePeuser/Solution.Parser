using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary><c>if (condition) ... else ...</c>. An <c>else if</c> is an <see cref="IfStatement"/> as <see cref="Else"/>.</summary>
    public sealed record IfStatement : Statement
    {
        public required Expression Condition { get; init; }

        public required Statement Then { get; init; }

        /// <summary>The <c>else</c> branch; null when there is none.</summary>
        public Statement? Else { get; init; }

        public bool HasElse => Else is not null;
    }

    /// <summary>
    /// <c>try { } catch { } finally { }</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// method.TryStatements.SelectMany(t =&gt; t.Catches).Where(c =&gt; c.CatchesAll &amp;&amp; !c.IsRethrowing)
    /// </code>
    /// </example>
    public sealed record TryStatement : Statement
    {
        /// <summary>The block after <c>try</c>.</summary>
        public required Statement Block { get; init; }

        public ImmutableList<CatchClause> Catches { get; init; } = ImmutableList<CatchClause>.Empty;

        /// <summary>The block after <c>finally</c>; null when there is none.</summary>
        public Statement? Finally { get; init; }

        public bool HasCatch => !Catches.IsEmpty;

        public bool HasFinally => Finally is not null;
    }

    /// <summary>One <c>catch (Exception ex) when (...) { }</c> of a <see cref="TryStatement"/>.</summary>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record CatchClause : CodeElement
    {
        /// <summary>The caught type; <see cref="TypeReference.None"/> for a bare <c>catch</c>.</summary>
        public required TypeReference ExceptionType { get; init; }

        /// <summary>The name of the caught exception, <c>ex</c>; null when none is given.</summary>
        public string? VariableName { get; init; }

        /// <summary>The condition of <c>when (...)</c>, without the parentheses; null when there is none.</summary>
        public Expression? Filter { get; init; }

        public required Statement Block { get; init; }

        /// <summary>True for a bare <c>catch</c> and for <c>catch (Exception)</c>, which catch everything.</summary>
        public bool CatchesAll => ExceptionType.Name.Length == 0 || ExceptionType.Is("System.Exception");

        /// <summary>True when the block has no statement at all, so the exception is swallowed silently.</summary>
        public bool IsEmpty => Block.Children.IsEmpty;

        /// <summary>True when the block contains a <c>throw;</c>, which passes the exception on with its stack trace.</summary>
        public bool IsRethrowing { get; init; }

        /// <summary>True when the block throws anything, a <c>throw;</c>, a <c>throw ex;</c> or a new exception.</summary>
        public bool Throws { get; init; }

        public bool HasFilter => Filter is not null;
    }

    /// <summary>
    /// <c>switch (subject) { case ...: ... }</c>. For the expression form <c>x switch { ... }</c> see
    /// <see cref="SwitchExpression"/>.
    /// </summary>
    public sealed record SwitchStatement : Statement
    {
        /// <summary>What is switched on.</summary>
        public required Expression Subject { get; init; }

        public ImmutableList<SwitchSection> Sections { get; init; } = ImmutableList<SwitchSection>.Empty;

        public bool HasDefault => Sections.Any(s => s.IsDefault);

        /// <summary>Every label of every section, in source order.</summary>
        public ImmutableList<SwitchLabel> Labels => Sections.SelectMany(s => s.Labels).ToImmutableList();
    }

    /// <summary>One or more labels with the statements they share.</summary>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record SwitchSection : CodeElement
    {
        public ImmutableList<SwitchLabel> Labels { get; init; } = ImmutableList<SwitchLabel>.Empty;

        public ImmutableList<Statement> Statements { get; init; } = ImmutableList<Statement>.Empty;

        public bool IsDefault => Labels.Any(l => l.IsDefault);
    }

    /// <summary><c>case 1:</c>, <c>case Foo f when f.IsValid:</c> or <c>default:</c>.</summary>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record SwitchLabel : CodeElement
    {
        /// <summary>The value or pattern after <c>case</c>, as written; null for <c>default</c>.</summary>
        public string? Pattern { get; init; }

        /// <summary>
        /// The pattern taken apart, see <see cref="CSharp.Pattern"/>; <see cref="Pattern"/> is the same as
        /// text. A <c>case 1:</c> is a constant pattern. Null for <c>default</c>.
        /// </summary>
        public CSharp.Pattern? TypedPattern { get; init; }

        /// <summary>The value classified, for a <c>case</c> with a constant such as <c>case "admin":</c>; null for a pattern and for <c>default</c>.</summary>
        public Expression? Value { get; init; }

        /// <summary>The condition after <c>when</c>; null when there is none.</summary>
        public Expression? WhenClause { get; init; }

        public bool IsDefault { get; init; }
    }

    /// <summary>
    /// A loop: <c>for</c>, <c>foreach</c> (also <c>await foreach</c>), <c>while</c> and <c>do</c>. The
    /// <see cref="Statement.Kind"/> tells which.
    /// </summary>
    public sealed record LoopStatement : Statement
    {
        /// <summary>The condition of a <c>for</c>, <c>while</c> or <c>do</c>; null for <c>foreach</c> and for <c>for (;;)</c>.</summary>
        public Expression? Condition { get; init; }

        /// <summary>What a <c>foreach</c> walks; null for any other loop.</summary>
        public Expression? Collection { get; init; }

        /// <summary>The variables a <c>for</c> or <c>foreach</c> declares.</summary>
        public ImmutableList<LocalVariable> Variables { get; init; } = ImmutableList<LocalVariable>.Empty;

        /// <summary>True for <c>await foreach</c>.</summary>
        public bool IsAwait { get; init; }

        public required Statement Body { get; init; }
    }

    /// <summary><c>using (var x = ...) { }</c> and <c>await using (...)</c>. A <c>using var</c> declaration is a <see cref="LocalDeclarationStatement"/>.</summary>
    public sealed record UsingStatement : Statement
    {
        /// <summary>What is disposed, as written: the declaration or the expression in the parentheses.</summary>
        public required string Resource { get; init; }

        /// <summary>The expression in <c>using (expression)</c> classified; null for a declaration, see <see cref="Variables"/>.</summary>
        public Expression? ResourceExpression { get; init; }

        public ImmutableList<LocalVariable> Variables { get; init; } = ImmutableList<LocalVariable>.Empty;

        public bool IsAwait { get; init; }

        public required Statement Body { get; init; }
    }

    /// <summary><c>lock (target) { }</c>.</summary>
    public sealed record LockStatement : Statement
    {
        public required Expression Target { get; init; }

        public required Statement Body { get; init; }
    }
}
