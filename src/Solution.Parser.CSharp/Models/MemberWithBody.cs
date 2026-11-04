using System.Collections.Immutable;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The shared shape of every member that can carry executable code.
    /// </summary>
    public abstract record MemberWithBody : DeclarationWithModifiers
    {
        /// <summary>
        /// The block including its braces, or the expression for an expression bodied member. Empty
        /// for an abstract, partial or extern member.
        /// </summary>
        public string Body { get; init; } = string.Empty;

        /// <summary>The statements of the body, one entry per statement, nested blocks kept intact.</summary>
        public ImmutableList<string> Statements { get; init; } = ImmutableList<string>.Empty;

        /// <summary>The non blank source lines of the body.</summary>
        public ImmutableList<string> LineStatements { get; init; } = ImmutableList<string>.Empty;

        /// <summary>
        /// The calls of <see cref="Invocations"/> grouped into fluent chains, as in
        /// <c>app.MapGet(...).WithName(...)</c>. Every call is in exactly one chain; a lone call is a
        /// chain of one.
        /// </summary>
        public ImmutableList<InvocationChain> InvocationChains => InvocationChain.Of(Invocations);

        public ImmutableList<LocalFunction> LocalFunctions { get; init; } = ImmutableList<LocalFunction>.Empty;

        /// <summary>
        /// The method calls in the body, in source order, including those inside lambdas. A local
        /// function reports its own.
        /// </summary>
        public ImmutableList<Invocation> Invocations { get; init; } = ImmutableList<Invocation>.Empty;

        /// <summary>
        /// The variables declared in the body, in source order, including those inside lambdas: plain
        /// declarations, <c>using var</c>, loop and catch variables, <c>out var</c> and pattern
        /// variables. A local function reports its own.
        /// </summary>
        public ImmutableList<LocalVariable> LocalVariables { get; init; } = ImmutableList<LocalVariable>.Empty;

        /// <summary>Every <c>new Foo()</c> and <c>new()</c> in the body, in source order, lambdas included.</summary>
        public ImmutableList<ObjectCreation> ObjectCreations { get; init; } = ImmutableList<ObjectCreation>.Empty;

        /// <summary>Every string written in the body, plain, verbatim, raw and interpolated, in source order, lambdas included.</summary>
        public ImmutableList<StringLiteral> StringLiterals { get; init; } = ImmutableList<StringLiteral>.Empty;

        /// <summary>Every <c>Target.Name</c> in the body, as in <c>DateTime.Now</c>, in source order, lambdas included.</summary>
        public ImmutableList<MemberAccess> MemberAccesses { get; init; } = ImmutableList<MemberAccess>.Empty;

        /// <summary>
        /// The statements of the body, typed and nested: an <c>if</c> holds its branches, a <c>try</c> its
        /// blocks. Empty for an expression bodied member. See <see cref="AllStatements"/> for a flat list.
        /// </summary>
        public ImmutableList<Statement> BodyStatements { get; init; } = ImmutableList<Statement>.Empty;

        /// <summary>
        /// Every statement of the body at any depth, in source order, those inside lambdas included. A
        /// local function reports its own. Filter it with <c>OfType&lt;IfStatement&gt;()</c> and the like,
        /// or use the shortcuts below.
        /// </summary>
        public ImmutableList<Statement> AllStatements { get; init; } = ImmutableList<Statement>.Empty;

        /// <summary>Every <c>if</c> of the body at any depth, the <c>if</c> of each <c>else if</c> included.</summary>
        public ImmutableList<IfStatement> IfStatements => Views.IfStatements;

        /// <summary>Every <c>for</c>, <c>foreach</c>, <c>while</c> and <c>do ... while</c> of the body at any depth.</summary>
        public ImmutableList<LoopStatement> Loops => Views.Loops;

        /// <summary>Every <c>for (...; ...; ...)</c> of the body at any depth.</summary>
        public ImmutableList<LoopStatement> ForLoops => Views.ForLoops;

        /// <summary>Every <c>foreach</c> of the body at any depth, <c>await foreach</c> included.</summary>
        public ImmutableList<LoopStatement> ForEachLoops => Views.ForEachLoops;

        /// <summary>Every <c>while (...) { }</c> of the body at any depth.</summary>
        public ImmutableList<LoopStatement> WhileLoops => Views.WhileLoops;

        /// <summary>Every <c>do { } while (...);</c> of the body at any depth.</summary>
        public ImmutableList<LoopStatement> DoWhileLoops => Views.DoWhileLoops;

        /// <summary>Every <c>try</c> of the body at any depth; its catches are <see cref="TryStatement.Catches"/>, read the way the code is written.</summary>
        /// <example>
        /// <code>
        /// from tryStatement in method.TryStatements
        /// from @catch in tryStatement.Catches
        /// where @catch.IsEmpty
        /// select $"{@catch.Location}: an empty catch swallows the exception";
        /// </code>
        /// </example>
        public ImmutableList<TryStatement> TryStatements => Views.TryStatements;

        /// <summary>Every <c>switch (...) { case ...: }</c> of the body at any depth; for <c>x switch { }</c> see <see cref="SwitchExpressions"/>.</summary>
        public ImmutableList<SwitchStatement> SwitchStatements => Views.SwitchStatements;

        /// <summary>Every <c>using (...) { }</c> of the body at any depth; a <c>using var</c> is a local variable, see <see cref="LocalVariables"/>.</summary>
        public ImmutableList<UsingStatement> UsingStatements => Views.UsingStatements;

        /// <summary>Every <c>lock (...) { }</c> of the body at any depth.</summary>
        public ImmutableList<LockStatement> LockStatements => Views.LockStatements;

        /// <summary>Every <c>return</c> of the body at any depth.</summary>
        public ImmutableList<ReturnStatement> Returns => Views.Returns;

        /// <summary>Every <c>throw</c> statement of the body at any depth; a <c>?? throw</c> is an expression, see <see cref="AllExpressions"/>.</summary>
        public ImmutableList<ThrowStatement> Throws => Views.Throws;

        /// <summary>True when the body has a <c>try</c> with at least one <c>catch</c>.</summary>
        public bool HasTryCatch => TryStatements.Any(t => t.HasCatch);

        private Unequated<Cached<StatementViews>> _views;

        /// <summary>Built once per member on first use, rather than on every access as a rule walks thousands of members.</summary>
        private StatementViews Views => Cache.For(ref _views, this, static member => new StatementViews(member.AllStatements, member.CollectExpressions));

        /// <summary>Every <c>x switch { ... }</c> in the body, lambdas included.</summary>
        public ImmutableList<SwitchExpression> SwitchExpressions { get; init; } = ImmutableList<SwitchExpression>.Empty;

        /// <summary>Every lambda and anonymous method in the body, nested ones included.</summary>
        public ImmutableList<Lambda> Lambdas { get; init; } = ImmutableList<Lambda>.Empty;

        /// <summary>The expression of an expression bodied member, <c>Total * 2</c> in <c>=&gt; Total * 2</c>; null for a block body.</summary>
        public Expression? ExpressionBody { get; init; }

        /// <summary>
        /// Every expression of the body at any depth, in source order per statement: the expressions
        /// of <see cref="AllStatements"/> and the <see cref="ExpressionBody"/>, each with everything
        /// inside it. A local function reports its own.
        /// </summary>
        /// <example>
        /// <code>
        /// method.AllExpressions.Where(e =&gt; e.Kind == ExpressionKind.Binary &amp;&amp; e.Operator is "==" or "!="
        ///                                  &amp;&amp; e.Right?.Kind == ExpressionKind.NullLiteral)
        /// </code>
        /// </example>
        public ImmutableList<Expression> AllExpressions => Views.AllExpressions;

        private ImmutableList<Expression> CollectExpressions()
        {
            // An expression body holds no statement, except those of a lambda with a block inside it.
            var statements = AllStatements.SelectMany(s => s.Expressions);
            var roots = ExpressionBody is null ? statements : statements.Prepend(ExpressionBody);

            return roots.SelectMany(e => e.DescendantsAndSelf()).ToImmutableList();
        }

        /// <summary>Every <c>await x</c> in the body, lambdas included; see <see cref="Expression.Operand"/> for what is awaited.</summary>
        public ImmutableList<Expression> Awaits { get; init; } = ImmutableList<Expression>.Empty;

        public ImmutableList<Parameter> Parameters { get; init; } = ImmutableList<Parameter>.Empty;

        /// <summary>True for <c>=&gt; expression</c> rather than a block body.</summary>
        public bool IsExpressionBodied { get; init; }

        /// <summary>True when the body yields, which makes the member an iterator.</summary>
        public bool IsIterator { get; init; }

        /// <summary>True when the member carries the <c>async</c> modifier.</summary>
        public bool IsAsync => Modifiers.Contains(Modifier.Async);

        public bool HasBody => Body.Length > 0;
    }
}
