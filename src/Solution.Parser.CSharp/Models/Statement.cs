using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// One statement, typed. The kinds a rule asks about most have their own record, so pattern
    /// matching reads like the code: <see cref="TryStatement"/>, <see cref="SwitchStatement"/>,
    /// <see cref="IfStatement"/>, <see cref="LoopStatement"/>, <see cref="ReturnStatement"/>,
    /// <see cref="ThrowStatement"/>, ... Every other kind is a plain <see cref="Statement"/> with its
    /// <see cref="Kind"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// from method in tree.AllMethods()
    /// from tryStatement in method.TryStatements
    /// from catchClause in tryStatement.Catches
    /// where catchClause.IsEmpty
    /// select $"{catchClause.Location}: an empty catch swallows the exception";
    /// </code>
    /// </example>
    [DebuggerDisplay("{Kind} {SourceCode}")]
    public record Statement : CodeElement
    {
        public StatementKind Kind { get; init; } = StatementKind.Other;

        /// <summary>
        /// The statements directly inside this one: those of a block, the branches of an <c>if</c>, the
        /// body of a loop, the blocks of a <c>try</c>. Use the member's <c>AllStatements</c> to get every
        /// statement at any depth.
        /// </summary>
        public ImmutableList<Statement> Children { get; init; } = ImmutableList<Statement>.Empty;

        /// <summary>The method calls inside this statement, nested statements and lambdas included, in source order.</summary>
        public ImmutableList<Invocation> Invocations { get; init; } = ImmutableList<Invocation>.Empty;

        /// <summary>
        /// The expressions this statement itself holds, not those of nested statements: the
        /// expression of an expression statement, the initializers of a declaration, the condition of
        /// an <c>if</c> or a loop, what is returned, thrown, yielded, switched on, walked, locked or
        /// disposed, the filters of the catches and the values and conditions of the case labels. Walk
        /// each with <see cref="Expression.DescendantsAndSelf"/>.
        /// </summary>
        public ImmutableList<Expression> Expressions
        {
            get
            {
                IEnumerable<Expression?> expressions = this switch
                {
                    ExpressionStatement e => [e.Expression],
                    LocalDeclarationStatement d => d.Variables.Select(v => v.InitializerExpression),
                    ReturnStatement r => [r.Value],
                    ThrowStatement t => [t.Value],
                    YieldStatement y => [y.Value],
                    IfStatement i => [i.Condition],
                    LoopStatement l => [..l.Variables.Select(v => v.InitializerExpression), l.Condition, l.Collection],
                    SwitchStatement s => [s.Subject, ..s.Labels.SelectMany(l => new[] { l.Value, l.WhenClause })],
                    TryStatement t => t.Catches.Select(c => c.Filter),
                    UsingStatement u => [u.ResourceExpression, ..u.Variables.Select(v => v.InitializerExpression)],
                    LockStatement l => [l.Target],
                    _ => []
                };

                return expressions.OfType<Expression>().ToImmutableList();
            }
        }
    }

    /// <summary>A call or an assignment on its own, as in <c>await SaveAsync();</c> or <c>x = 3;</c>.</summary>
    public sealed record ExpressionStatement : Statement
    {
        public required Expression Expression { get; init; }
    }

    /// <summary><c>var x = 1, y = 2;</c>, <c>using var stream = ...;</c> and <c>const int Max = 3;</c>.</summary>
    public sealed record LocalDeclarationStatement : Statement
    {
        public ImmutableList<LocalVariable> Variables { get; init; } = ImmutableList<LocalVariable>.Empty;
    }

    /// <summary><c>return</c> with or without a value.</summary>
    public sealed record ReturnStatement : Statement
    {
        /// <summary>What is returned; null for a plain <c>return;</c>.</summary>
        public Expression? Value { get; init; }
    }

    /// <summary><c>yield return x;</c> and <c>yield break;</c>.</summary>
    public sealed record YieldStatement : Statement
    {
        /// <summary>What is yielded; null for <c>yield break</c>.</summary>
        public Expression? Value { get; init; }

        public bool IsBreak => Kind == StatementKind.YieldBreak;
    }

    /// <summary><c>throw new Foo();</c> and the rethrowing <c>throw;</c>.</summary>
    public sealed record ThrowStatement : Statement
    {
        /// <summary>What is thrown; null for <c>throw;</c>.</summary>
        public Expression? Value { get; init; }

        /// <summary>True for <c>throw;</c>, which keeps the stack trace of the caught exception.</summary>
        public bool IsRethrow => Value is null;

        /// <summary>The type of a <c>throw new Foo(...)</c>; null when something else is thrown, as in <c>throw ex;</c>.</summary>
        public TypeReference? ExceptionType => Value is { Kind: ExpressionKind.ObjectCreation } ? Value.TypeReference : null;
    }

    public enum StatementKind
    {
        Other,

        /// <summary><c>{ ... }</c>.</summary>
        Block,

        /// <summary>See <see cref="ExpressionStatement"/>.</summary>
        Expression,

        /// <summary>See <see cref="LocalDeclarationStatement"/>.</summary>
        LocalDeclaration,

        /// <summary>See <see cref="IfStatement"/>.</summary>
        If,

        /// <summary>See <see cref="SwitchStatement"/>.</summary>
        Switch,

        /// <summary>See <see cref="TryStatement"/>.</summary>
        Try,

        /// <summary>See <see cref="ReturnStatement"/>.</summary>
        Return,

        /// <summary>See <see cref="ThrowStatement"/>.</summary>
        Throw,

        /// <summary>See <see cref="YieldStatement"/>.</summary>
        YieldReturn,

        /// <summary>See <see cref="YieldStatement"/>.</summary>
        YieldBreak,

        /// <summary>See <see cref="LoopStatement"/>.</summary>
        For,

        /// <summary>See <see cref="LoopStatement"/>, also <c>await foreach</c>.</summary>
        ForEach,

        /// <summary>See <see cref="LoopStatement"/>.</summary>
        While,

        /// <summary>See <see cref="LoopStatement"/>.</summary>
        Do,

        /// <summary>See <see cref="UsingStatement"/>.</summary>
        Using,

        /// <summary>See <see cref="LockStatement"/>.</summary>
        Lock,

        Break,

        Continue,

        Goto,

        /// <summary>A lone <c>;</c>.</summary>
        Empty,

        /// <summary>A local function; it reports its own body, see <c>LocalFunctions</c>.</summary>
        LocalFunction,

        Labeled,

        Checked,

        Unsafe,

        Fixed
    }
}
