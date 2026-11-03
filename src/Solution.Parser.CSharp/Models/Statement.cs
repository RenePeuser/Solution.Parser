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
    /// from method in tree.Methods
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
        /// Every statement inside this one at any depth, in source order, those inside lambdas included,
        /// this statement itself excluded. A local function reports its own. It is what a member's
        /// <c>AllStatements</c> is for the whole body, so every question a rule asks a method it can ask
        /// one statement as well.
        /// </summary>
        /// <example>
        /// <code>
        /// // a loop inside a loop
        /// from method in trees.Methods
        /// from loop in method.Loops
        /// from inner in loop.Loops
        /// select $"{inner.Location}: {method.Name} nests loops";
        /// </code>
        /// </example>
        public ImmutableList<Statement> AllStatements => Views.All;

        /// <summary>Every <c>if</c> inside this statement at any depth; for an <c>if</c> that includes its <c>else if</c> branches.</summary>
        public ImmutableList<IfStatement> IfStatements => Views.IfStatements;

        /// <summary>Every <c>for</c>, <c>foreach</c>, <c>while</c> and <c>do ... while</c> inside this statement at any depth.</summary>
        public ImmutableList<LoopStatement> Loops => Views.Loops;

        /// <summary>Every <c>for (...; ...; ...)</c> inside this statement at any depth.</summary>
        public ImmutableList<LoopStatement> ForLoops => Views.ForLoops;

        /// <summary>Every <c>foreach</c> inside this statement at any depth, <c>await foreach</c> included.</summary>
        public ImmutableList<LoopStatement> ForEachLoops => Views.ForEachLoops;

        /// <summary>Every <c>while (...) { }</c> inside this statement at any depth.</summary>
        public ImmutableList<LoopStatement> WhileLoops => Views.WhileLoops;

        /// <summary>Every <c>do { } while (...);</c> inside this statement at any depth.</summary>
        public ImmutableList<LoopStatement> DoWhileLoops => Views.DoWhileLoops;

        /// <summary>Every <c>try</c> inside this statement at any depth.</summary>
        public ImmutableList<TryStatement> TryStatements => Views.TryStatements;

        /// <summary>
        /// Every <c>catch</c> of every <c>try</c> inside this statement. The catches of a
        /// <see cref="TryStatement"/> itself are its <see cref="TryStatement.Catches"/>.
        /// </summary>
        public ImmutableList<CatchClause> CatchClauses => Views.CatchClauses;

        /// <summary>Every <c>switch (...) { case ...: }</c> inside this statement at any depth.</summary>
        public ImmutableList<SwitchStatement> SwitchStatements => Views.SwitchStatements;

        /// <summary>Every <c>using (...) { }</c> inside this statement at any depth.</summary>
        public ImmutableList<UsingStatement> UsingStatements => Views.UsingStatements;

        /// <summary>Every <c>return</c> inside this statement at any depth.</summary>
        public ImmutableList<ReturnStatement> Returns => Views.Returns;

        /// <summary>Every <c>throw</c> statement inside this statement at any depth.</summary>
        /// <example>
        /// <code>
        /// // throw ex; loses the stack trace, throw; keeps it
        /// from catchClause in method.CatchClauses
        /// from throwStatement in catchClause.Block.Throws
        /// where catchClause.VariableName is not null &amp;&amp; throwStatement.Value?.Name == catchClause.VariableName
        /// select $"{throwStatement.Location}: write throw; to keep the stack trace";
        /// </code>
        /// </example>
        public ImmutableList<ThrowStatement> Throws => Views.Throws;

        /// <summary>
        /// Every expression of this statement and of the statements inside it, at any depth: what
        /// <see cref="Expressions"/> is for this statement alone, walked all the way down.
        /// </summary>
        public ImmutableList<Expression> AllExpressions => Views.AllExpressions;

        /// <summary>Every <c>await x</c> inside this statement, lambdas included; see <see cref="Expression.Operand"/> for what is awaited.</summary>
        public ImmutableList<Expression> Awaits => Contents.Awaits;

        /// <summary>The variables declared inside this statement: those of its own head, as in a <c>foreach</c> or <c>using</c>, and those of the statements inside it.</summary>
        public ImmutableList<LocalVariable> LocalVariables => Contents.LocalVariables;

        /// <summary>Every <c>new Foo()</c> and <c>new()</c> inside this statement, lambdas included.</summary>
        public ImmutableList<ObjectCreation> ObjectCreations => Contents.ObjectCreations;

        /// <summary>Every string written inside this statement, lambdas included.</summary>
        public ImmutableList<StringLiteral> StringLiterals => Contents.StringLiterals;

        /// <summary>Every <c>Target.Name</c> inside this statement, as in <c>DateTime.Now</c>, lambdas included.</summary>
        public ImmutableList<MemberAccess> MemberAccesses => Contents.MemberAccesses;

        /// <summary>Every lambda and anonymous method inside this statement, nested ones included.</summary>
        public ImmutableList<Lambda> Lambdas => Contents.Lambdas;

        /// <summary>Every <c>x switch { ... }</c> inside this statement, lambdas included.</summary>
        public ImmutableList<SwitchExpression> SwitchExpressions => Contents.SwitchExpressions;

        private Unequated<BodyIndex> _index;

        private Unequated<Cached<StatementViews>> _views;

        private Unequated<Cached<StatementContents>> _contents;

        /// <summary>Hands the statement the lists of the body it belongs to, before the body is given out.</summary>
        internal void Attach(BodyIndex index)
        {
            _index.Value = index;
        }

        private StatementViews Views => Cache.For(ref _views, this, static statement => new StatementViews(statement.CollectStatements(), statement.CollectExpressions));

        private StatementContents Contents => Cache.For(ref _contents, this, static statement => new StatementContents(statement, statement._index.Value ?? new BodyIndex()));

        private ImmutableList<Statement> CollectStatements()
        {
            // Without the body, a statement built by hand or copied with 'with', the children are all
            // there is to go by; the statements of lambdas are not among them.
            return _index.Value?.StatementsInside(this) ??
                   Children.SelectMany(child => child.AllStatements.Prepend(child)).ToImmutableList();
        }

        private ImmutableList<Expression> CollectExpressions()
        {
            return AllStatements.Prepend(this).SelectMany(s => s.Expressions).SelectMany(e => e.DescendantsAndSelf()).ToImmutableList();
        }

        /// <summary>What a statement holds besides statements, cut out of the body's lists on first use.</summary>
        private sealed class StatementContents(Statement statement, BodyIndex index)
        {
            internal ImmutableList<Expression> Awaits => field ??= index.AwaitsIn(statement);

            internal ImmutableList<LocalVariable> LocalVariables => field ??= index.LocalVariablesIn(statement);

            internal ImmutableList<ObjectCreation> ObjectCreations => field ??= index.ObjectCreationsIn(statement);

            internal ImmutableList<StringLiteral> StringLiterals => field ??= index.StringLiteralsIn(statement);

            internal ImmutableList<MemberAccess> MemberAccesses => field ??= index.MemberAccessesIn(statement);

            internal ImmutableList<Lambda> Lambdas => field ??= index.LambdasIn(statement);

            internal ImmutableList<SwitchExpression> SwitchExpressions => field ??= index.SwitchExpressionsIn(statement);
        }

        /// <summary>
        /// True when the statement leaves for good at its end: a <c>return</c>, <c>throw</c>,
        /// <c>break</c>, <c>continue</c>, <c>goto</c> or <c>yield break</c>, or a block whose last statement
        /// does. An <c>else</c> after such a branch is not needed, the code can go on after the <c>if</c>.
        /// </summary>
        public bool EndsWithJump => Kind is StatementKind.Return or StatementKind.Throw or StatementKind.Break
                                        or StatementKind.Continue or StatementKind.Goto or StatementKind.YieldBreak ||
                                    (Kind == StatementKind.Block && Children.LastOrDefault()?.EndsWithJump == true);

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
