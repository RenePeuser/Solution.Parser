using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Turns the statements of one body into the typed records. Each statement is built once and then
    /// shared, so the nested <see cref="Statement.Children"/> and the flat <c>AllStatements</c> hold the
    /// same instances. The calls and variables of a statement are cut out of the member's lists by
    /// position rather than collected again, which would walk nested statements over and over.
    /// </summary>
    internal sealed class StatementBuilder(string filePath, ImmutableList<Invocation> invocations, ImmutableList<LocalVariable> localVariables)
    {
        private readonly Dictionary<StatementSyntax, Statement> _built = new();

        // Both lists are in source order; arrays make the binary search in InSpan cheap.
        private readonly Invocation[] _invocations = invocations.ToArray();

        private readonly LocalVariable[] _variables = localVariables.OrderBy(v => v.Location.SpanStart).ToArray();

        internal ImmutableList<Statement> Build(IEnumerable<StatementSyntax> statements)
        {
            return statements.Select(Build).ToImmutableList();
        }

        internal Statement Build(StatementSyntax statement)
        {
            if (!_built.TryGetValue(statement, out var built))
            {
                built = Create(statement, new Common(statement, statement.ToCodeLocation(filePath), InvocationsIn(statement)));

                _built[statement] = built;
            }

            return built;
        }

        internal Lambda BuildLambda(AnonymousFunctionExpressionSyntax lambda)
        {
            var parameters = lambda switch
            {
                SimpleLambdaExpressionSyntax simple => ImmutableList.Create(simple.Parameter.ToParameter(0, filePath)),
                ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.ToParameters(filePath),
                AnonymousMethodExpressionSyntax anonymous => anonymous.ParameterList.ToParameters(filePath),
                _ => ImmutableList<Parameter>.Empty
            };

            return new Lambda
            {
                Parameters = parameters,
                IsAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword),
                IsStatic = lambda.Modifiers.Any(SyntaxKind.StaticKeyword),
                IsExpressionBodied = lambda.Block is null,
                BodyNode = lambda.Body,
                Statements = lambda.Block is null ? ImmutableList<Statement>.Empty : Build(lambda.Block.Statements),
                Invocations = InvocationsIn(lambda),
                SourceNode = lambda,
                FilePath = filePath,
                Location = lambda.ToCodeLocation(filePath)
            };
        }

        internal SwitchExpression BuildSwitchExpression(SwitchExpressionSyntax switchExpression)
        {
            return new SwitchExpression
            {
                Subject = switchExpression.GoverningExpression.ToExpression(filePath),
                Arms = switchExpression.Arms
                                       .Select(arm => new SwitchExpressionArm
                                       {
                                           Pattern = arm.Pattern.ToSourceCode(),
                                           WhenClause = arm.WhenClause?.Condition.ToExpression(filePath),
                                           Result = arm.Expression.ToExpression(filePath),
                                           IsDiscard = arm.Pattern is DiscardPatternSyntax && arm.WhenClause is null,
                                           SourceNode = arm,
                                           FilePath = filePath,
                                           Location = arm.ToCodeLocation(filePath)
                                       })
                                       .ToImmutableList(),
                SourceNode = switchExpression,
                FilePath = filePath,
                Location = switchExpression.ToCodeLocation(filePath)
            };
        }

        private Statement Create(StatementSyntax statement, Common c)
        {
            return statement switch
            {
                BlockSyntax block => Plain(StatementKind.Block, c, Build(block.Statements)),

                ExpressionStatementSyntax expression => new ExpressionStatement
                {
                    Kind = StatementKind.Expression,
                    Expression = expression.Expression.ToExpression(filePath),
                    SourceNode = c.Node,
                    Location = c.Location,
                    Invocations = c.Invocations,
                    FilePath = filePath
                },

                LocalDeclarationStatementSyntax declaration => new LocalDeclarationStatement
                {
                    Kind = StatementKind.LocalDeclaration,
                    Variables = DeclaredBy(declaration.Declaration),
                    SourceNode = c.Node,
                    Location = c.Location,
                    Invocations = c.Invocations,
                    FilePath = filePath
                },

                IfStatementSyntax ifStatement => CreateIf(ifStatement, c),

                SwitchStatementSyntax switchStatement => CreateSwitch(switchStatement, c),

                TryStatementSyntax tryStatement => CreateTry(tryStatement, c),

                ReturnStatementSyntax returnStatement => new ReturnStatement
                {
                    Kind = StatementKind.Return,
                    Value = returnStatement.Expression.ToExpressionOrNull(filePath),
                    SourceNode = c.Node,
                    Location = c.Location,
                    Invocations = c.Invocations,
                    FilePath = filePath
                },

                ThrowStatementSyntax throwStatement => new ThrowStatement
                {
                    Kind = StatementKind.Throw,
                    Value = throwStatement.Expression.ToExpressionOrNull(filePath),
                    SourceNode = c.Node,
                    Location = c.Location,
                    Invocations = c.Invocations,
                    FilePath = filePath
                },

                YieldStatementSyntax yieldStatement => new YieldStatement
                {
                    Kind = yieldStatement.IsKind(SyntaxKind.YieldBreakStatement) ? StatementKind.YieldBreak : StatementKind.YieldReturn,
                    Value = yieldStatement.Expression.ToExpressionOrNull(filePath),
                    SourceNode = c.Node,
                    Location = c.Location,
                    Invocations = c.Invocations,
                    FilePath = filePath
                },

                ForStatementSyntax forStatement => Loop(StatementKind.For, forStatement.Statement, forStatement, c) with { Condition = forStatement.Condition.ToExpressionOrNull(filePath) },

                CommonForEachStatementSyntax forEach => Loop(StatementKind.ForEach, forEach.Statement, forEach, c) with { Collection = forEach.Expression.ToExpression(filePath), IsAwait = forEach.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword) },

                WhileStatementSyntax whileStatement => Loop(StatementKind.While, whileStatement.Statement, whileStatement, c) with { Condition = whileStatement.Condition.ToExpression(filePath) },

                DoStatementSyntax doStatement => Loop(StatementKind.Do, doStatement.Statement, doStatement, c) with { Condition = doStatement.Condition.ToExpression(filePath) },

                UsingStatementSyntax usingStatement => new UsingStatement
                {
                    Kind = StatementKind.Using,
                    Resource = ((SyntaxNode?)usingStatement.Declaration ?? usingStatement.Expression)?.ToSourceCode() ?? string.Empty,
                    Variables = VariablesBefore(usingStatement, usingStatement.Statement),
                    IsAwait = usingStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword),
                    Body = Build(usingStatement.Statement),
                    Children = ImmutableList.Create(Build(usingStatement.Statement)),
                    SourceNode = c.Node,
                    Location = c.Location,
                    Invocations = c.Invocations,
                    FilePath = filePath
                },

                LockStatementSyntax lockStatement => new LockStatement
                {
                    Kind = StatementKind.Lock,
                    Target = lockStatement.Expression.ToExpression(filePath),
                    Body = Build(lockStatement.Statement),
                    Children = ImmutableList.Create(Build(lockStatement.Statement)),
                    SourceNode = c.Node,
                    Location = c.Location,
                    Invocations = c.Invocations,
                    FilePath = filePath
                },

                BreakStatementSyntax => Plain(StatementKind.Break, c),
                ContinueStatementSyntax => Plain(StatementKind.Continue, c),
                GotoStatementSyntax => Plain(StatementKind.Goto, c),
                EmptyStatementSyntax => Plain(StatementKind.Empty, c),
                LocalFunctionStatementSyntax => Plain(StatementKind.LocalFunction, c),
                LabeledStatementSyntax labeled => Plain(StatementKind.Labeled, c, ImmutableList.Create(Build(labeled.Statement))),
                CheckedStatementSyntax checkedStatement => Plain(StatementKind.Checked, c, ImmutableList.Create(Build(checkedStatement.Block))),
                UnsafeStatementSyntax unsafeStatement => Plain(StatementKind.Unsafe, c, ImmutableList.Create(Build(unsafeStatement.Block))),
                FixedStatementSyntax fixedStatement => Plain(StatementKind.Fixed, c, ImmutableList.Create(Build(fixedStatement.Statement))),
                _ => Plain(StatementKind.Other, c)
            };
        }

        private IfStatement CreateIf(IfStatementSyntax ifStatement, Common c)
        {
            var then = Build(ifStatement.Statement);
            var otherwise = ifStatement.Else is null ? null : Build(ifStatement.Else.Statement);

            return new IfStatement
            {
                Kind = StatementKind.If,
                Condition = ifStatement.Condition.ToExpression(filePath),
                Then = then,
                Else = otherwise,
                Children = otherwise is null ? ImmutableList.Create(then) : ImmutableList.Create(then, otherwise),
                SourceNode = c.Node,
                Location = c.Location,
                Invocations = c.Invocations,
                FilePath = filePath
            };
        }

        private SwitchStatement CreateSwitch(SwitchStatementSyntax switchStatement, Common c)
        {
            var sections = switchStatement.Sections
                                          .Select(section => new SwitchSection
                                          {
                                              Labels = section.Labels.Select(ToLabel).ToImmutableList(),
                                              Statements = Build(section.Statements),
                                              SourceNode = section,
                                              FilePath = filePath,
                                              Location = section.ToCodeLocation(filePath)
                                          })
                                          .ToImmutableList();

            return new SwitchStatement
            {
                Kind = StatementKind.Switch,
                Subject = switchStatement.Expression.ToExpression(filePath),
                Sections = sections,
                Children = sections.SelectMany(s => s.Statements).ToImmutableList(),
                SourceNode = c.Node,
                Location = c.Location,
                Invocations = c.Invocations,
                FilePath = filePath
            };
        }

        private SwitchLabel ToLabel(SwitchLabelSyntax label)
        {
            var (pattern, value, when) = label switch
            {
                CaseSwitchLabelSyntax caseLabel => (caseLabel.Value.ToSourceCode(), caseLabel.Value.ToExpression(filePath), (Expression?)null),
                CasePatternSwitchLabelSyntax patternLabel => (patternLabel.Pattern.ToSourceCode(),
                                                              patternLabel.Pattern is ConstantPatternSyntax constant ? constant.Expression.ToExpression(filePath) : null,
                                                              patternLabel.WhenClause?.Condition.ToExpression(filePath)),
                _ => ((string?)null, (Expression?)null, (Expression?)null)
            };

            return new SwitchLabel
            {
                Pattern = pattern,
                Value = value,
                WhenClause = when,
                IsDefault = label is DefaultSwitchLabelSyntax,
                SourceNode = label,
                FilePath = filePath,
                Location = label.ToCodeLocation(filePath)
            };
        }

        private TryStatement CreateTry(TryStatementSyntax tryStatement, Common c)
        {
            var block = Build(tryStatement.Block);
            var finallyBlock = tryStatement.Finally is null ? null : Build(tryStatement.Finally.Block);

            var catches = tryStatement.Catches
                                      .Select(clause =>
                                      {
                                          // A throw inside a lambda in the catch block does not leave the catch.
                                          var throws = clause.Block.BodyNodes()
                                                             .Where(n => n is ThrowStatementSyntax or ThrowExpressionSyntax)
                                                             .Where(n => n.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() is not { } lambda ||
                                                                         !clause.Block.Span.Contains(lambda.Span))
                                                             .ToList();

                                          return new CatchClause
                                          {
                                              ExceptionType = clause.Declaration?.Type.ToTypeReference(filePath) ?? TypeReference.None,
                                              VariableName = clause.Declaration?.Identifier.ValueText is { Length: > 0 } name ? name : null,
                                              Filter = clause.Filter?.FilterExpression.ToExpression(filePath),
                                              Block = Build(clause.Block),
                                              IsRethrowing = throws.Any(t => t is ThrowStatementSyntax { Expression: null }),
                                              Throws = throws.Count > 0,
                                              SourceNode = clause,
                                              FilePath = filePath,
                                              Location = clause.ToCodeLocation(filePath)
                                          };
                                      })
                                      .ToImmutableList();

            IEnumerable<Statement> children = [block, ..catches.Select(c => c.Block), ..(finallyBlock is null ? [] : new[] { finallyBlock })];

            return new TryStatement
            {
                Kind = StatementKind.Try,
                Block = block,
                Catches = catches,
                Finally = finallyBlock,
                Children = children.ToImmutableList(),
                SourceNode = c.Node,
                Location = c.Location,
                Invocations = c.Invocations,
                FilePath = filePath
            };
        }

        private LoopStatement Loop(StatementKind kind, StatementSyntax body, StatementSyntax loop, Common c)
        {
            var built = Build(body);

            return new LoopStatement
            {
                Kind = kind,
                Body = built,
                Variables = VariablesBefore(loop, body),
                Children = ImmutableList.Create(built),
                SourceNode = c.Node,
                Location = c.Location,
                Invocations = c.Invocations,
                FilePath = filePath
            };
        }

        /// <summary>The variables a statement declares in its head, before its body starts.</summary>
        private ImmutableList<LocalVariable> VariablesBefore(SyntaxNode statement, SyntaxNode body)
        {
            return SpanSearch.InSpan(_variables, v => v.Location.SpanStart, statement.SpanStart, body.SpanStart);
        }

        /// <summary>
        /// The variables of the declaration itself, not those of a pattern or lambda in an initializer,
        /// as in <c>Func&lt;int&gt; f = () =&gt; { var x = 1; return x; };</c>, which declares <c>f</c> only.
        /// </summary>
        private ImmutableList<LocalVariable> DeclaredBy(VariableDeclarationSyntax declaration)
        {
            var declarators = declaration.Variables.Select(v => v.SpanStart).ToHashSet();

            return SpanSearch.InSpan(_variables, v => v.Location.SpanStart, declaration.SpanStart, declaration.Span.End)
                .Where(v => declarators.Contains(v.Location.SpanStart))
                .ToImmutableList();
        }

        private ImmutableList<Invocation> InvocationsIn(SyntaxNode node)
        {
            return SpanSearch.InSpan(_invocations, i => i.Location.SpanStart, node.SpanStart, node.Span.End);
        }

        private Statement Plain(StatementKind kind, Common c, ImmutableList<Statement>? children = null)
        {
            return new Statement
            {
                Kind = kind,
                Children = children ?? ImmutableList<Statement>.Empty,
                SourceNode = c.Node,
                FilePath = filePath,
                Location = c.Location,
                Invocations = c.Invocations
            };
        }

        /// <summary>What every statement carries, computed once in <see cref="Build(StatementSyntax)"/>.</summary>
        private readonly record struct Common(StatementSyntax Node, CodeLocation Location, ImmutableList<Invocation> Invocations);
    }
}
