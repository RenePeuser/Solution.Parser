using System;
using System.Collections.Immutable;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The statements of a body or of one statement sorted by what they are, each list built on first
    /// use and then kept. A member and a statement share it, so <c>method.ForEachLoops</c> and
    /// <c>loop.ForEachLoops</c> mean the same, just for a different stretch of code.
    /// </summary>
    internal sealed class StatementViews(ImmutableList<Statement> statements, Func<ImmutableList<Expression>> expressions)
    {
        internal ImmutableList<Statement> All => statements;

        internal ImmutableList<IfStatement> IfStatements => field ??= statements.OfType<IfStatement>().ToImmutableList();

        internal ImmutableList<LoopStatement> Loops => field ??= statements.OfType<LoopStatement>().ToImmutableList();

        internal ImmutableList<LoopStatement> ForLoops => field ??= LoopsOf(StatementKind.For);

        internal ImmutableList<LoopStatement> ForEachLoops => field ??= LoopsOf(StatementKind.ForEach);

        internal ImmutableList<LoopStatement> WhileLoops => field ??= LoopsOf(StatementKind.While);

        internal ImmutableList<LoopStatement> DoWhileLoops => field ??= LoopsOf(StatementKind.Do);

        internal ImmutableList<TryStatement> TryStatements => field ??= statements.OfType<TryStatement>().ToImmutableList();

        internal ImmutableList<SwitchStatement> SwitchStatements => field ??= statements.OfType<SwitchStatement>().ToImmutableList();

        internal ImmutableList<UsingStatement> UsingStatements => field ??= statements.OfType<UsingStatement>().ToImmutableList();

        internal ImmutableList<LockStatement> LockStatements => field ??= statements.OfType<LockStatement>().ToImmutableList();

        internal ImmutableList<ReturnStatement> Returns => field ??= statements.OfType<ReturnStatement>().ToImmutableList();

        internal ImmutableList<ThrowStatement> Throws => field ??= statements.OfType<ThrowStatement>().ToImmutableList();

        internal ImmutableList<Expression> AllExpressions => field ??= expressions();

        private ImmutableList<LoopStatement> LoopsOf(StatementKind kind)
        {
            return Loops.Where(l => l.Kind == kind).ToImmutableList();
        }
    }
}
