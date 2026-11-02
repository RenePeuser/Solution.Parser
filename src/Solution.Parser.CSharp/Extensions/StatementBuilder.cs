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
    /// same instances. The calls of a statement are cut out of the member's calls by position rather
    /// than collected again.
    /// </summary>
    internal sealed class StatementBuilder(string filePath, ImmutableList<Invocation> invocations)
    {
        private readonly Dictionary<StatementSyntax, Statement> _built = new();

        internal ImmutableList<Statement> Build(IEnumerable<StatementSyntax> statements)
        {
            return statements.Select(Build).ToImmutableList();
        }

        internal Statement Build(StatementSyntax statement)
        {
            if (!_built.TryGetValue(statement, out var built))
            {
                built = Create(statement) with
                {
                    SourceCode = statement.ToSourceCode(),
                    FilePath = filePath,
                    Location = statement.ToCodeLocation(filePath),
                    Invocations = InvocationsIn(statement)
                };

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
                Body = lambda.Body.ToSourceCode(),
                Statements = lambda.Block is null ? ImmutableList<Statement>.Empty : Build(lambda.Block.Statements),
                Invocations = InvocationsIn(lambda),
                SourceCode = lambda.ToSourceCode(),
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
                                           SourceCode = arm.ToSourceCode(),
                                           FilePath = filePath,
                                           Location = arm.ToCodeLocation(filePath)
                                       })
                                       .ToImmutableList(),
                SourceCode = switchExpression.ToSourceCode(),
                FilePath = filePath,
                Location = switchExpression.ToCodeLocation(filePath)
            };
        }

        private Statement Create(StatementSyntax statement)
        {
            return statement switch
            {
                BlockSyntax block => Plain(StatementKind.Block) with { Children = Build(block.Statements) },

                ExpressionStatementSyntax expression => new ExpressionStatement
                {
                    Kind = StatementKind.Expression,
                    Expression = expression.Expression.ToExpression(filePath),
                    SourceCode = string.Empty,
                    FilePath = filePath
                },

                LocalDeclarationStatementSyntax declaration => new LocalDeclarationStatement
                {
                    Kind = StatementKind.LocalDeclaration,
                    Variables = declaration.ToLocalVariables(filePath).Where(v => v.Kind == LocalVariableKind.Declaration).ToImmutableList(),
                    SourceCode = string.Empty,
                    FilePath = filePath
                },

                IfStatementSyntax ifStatement => CreateIf(ifStatement),

                SwitchStatementSyntax switchStatement => CreateSwitch(switchStatement),

                TryStatementSyntax tryStatement => CreateTry(tryStatement),

                ReturnStatementSyntax returnStatement => new ReturnStatement
                {
                    Kind = StatementKind.Return,
                    Value = returnStatement.Expression.ToExpressionOrNull(filePath),
                    SourceCode = string.Empty,
                    FilePath = filePath
                },

                ThrowStatementSyntax throwStatement => new ThrowStatement
                {
                    Kind = StatementKind.Throw,
                    Value = throwStatement.Expression.ToExpressionOrNull(filePath),
                    SourceCode = string.Empty,
                    FilePath = filePath
                },

                YieldStatementSyntax yieldStatement => new YieldStatement
                {
                    Kind = yieldStatement.IsKind(SyntaxKind.YieldBreakStatement) ? StatementKind.YieldBreak : StatementKind.YieldReturn,
                    Value = yieldStatement.Expression.ToExpressionOrNull(filePath),
                    SourceCode = string.Empty,
                    FilePath = filePath
                },

                ForStatementSyntax forStatement => Loop(StatementKind.For, forStatement.Statement, forStatement) with
                {
                    Condition = forStatement.Condition.ToExpressionOrNull(filePath)
                },

                CommonForEachStatementSyntax forEach => Loop(StatementKind.ForEach, forEach.Statement, forEach) with
                {
                    Collection = forEach.Expression.ToExpression(filePath),
                    IsAwait = forEach.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)
                },

                WhileStatementSyntax whileStatement => Loop(StatementKind.While, whileStatement.Statement, whileStatement) with
                {
                    Condition = whileStatement.Condition.ToExpression(filePath)
                },

                DoStatementSyntax doStatement => Loop(StatementKind.Do, doStatement.Statement, doStatement) with
                {
                    Condition = doStatement.Condition.ToExpression(filePath)
                },

                UsingStatementSyntax usingStatement => new UsingStatement
                {
                    Kind = StatementKind.Using,
                    Resource = ((SyntaxNode?)usingStatement.Declaration ?? usingStatement.Expression)?.ToSourceCode() ?? string.Empty,
                    Variables = VariablesBefore(usingStatement, usingStatement.Statement),
                    IsAwait = usingStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword),
                    Body = Build(usingStatement.Statement),
                    Children = ImmutableList.Create(Build(usingStatement.Statement)),
                    SourceCode = string.Empty,
                    FilePath = filePath
                },

                LockStatementSyntax lockStatement => new LockStatement
                {
                    Kind = StatementKind.Lock,
                    Target = lockStatement.Expression.ToExpression(filePath),
                    Body = Build(lockStatement.Statement),
                    Children = ImmutableList.Create(Build(lockStatement.Statement)),
                    SourceCode = string.Empty,
                    FilePath = filePath
                },

                BreakStatementSyntax => Plain(StatementKind.Break),
                ContinueStatementSyntax => Plain(StatementKind.Continue),
                GotoStatementSyntax => Plain(StatementKind.Goto),
                EmptyStatementSyntax => Plain(StatementKind.Empty),
                LocalFunctionStatementSyntax => Plain(StatementKind.LocalFunction),
                LabeledStatementSyntax labeled => Plain(StatementKind.Labeled) with { Children = ImmutableList.Create(Build(labeled.Statement)) },
                CheckedStatementSyntax checkedStatement => Plain(StatementKind.Checked) with { Children = ImmutableList.Create(Build(checkedStatement.Block)) },
                UnsafeStatementSyntax unsafeStatement => Plain(StatementKind.Unsafe) with { Children = ImmutableList.Create(Build(unsafeStatement.Block)) },
                FixedStatementSyntax fixedStatement => Plain(StatementKind.Fixed) with { Children = ImmutableList.Create(Build(fixedStatement.Statement)) },
                _ => Plain(StatementKind.Other)
            };
        }

        private IfStatement CreateIf(IfStatementSyntax ifStatement)
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
                SourceCode = string.Empty,
                FilePath = filePath
            };
        }

        private SwitchStatement CreateSwitch(SwitchStatementSyntax switchStatement)
        {
            var sections = switchStatement.Sections
                                          .Select(section => new SwitchSection
                                          {
                                              Labels = section.Labels.Select(ToLabel).ToImmutableList(),
                                              Statements = Build(section.Statements),
                                              SourceCode = section.ToSourceCode(),
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
                SourceCode = string.Empty,
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
                SourceCode = label.ToSourceCode(),
                FilePath = filePath,
                Location = label.ToCodeLocation(filePath)
            };
        }

        private TryStatement CreateTry(TryStatementSyntax tryStatement)
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
                                              SourceCode = clause.ToSourceCode(),
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
                SourceCode = string.Empty,
                FilePath = filePath
            };
        }

        private LoopStatement Loop(StatementKind kind, StatementSyntax body, StatementSyntax loop)
        {
            var built = Build(body);

            return new LoopStatement
            {
                Kind = kind,
                Body = built,
                Variables = VariablesBefore(loop, body),
                Children = ImmutableList.Create(built),
                SourceCode = string.Empty,
                FilePath = filePath
            };
        }

        /// <summary>The variables a statement declares in its head, before its body starts.</summary>
        private ImmutableList<LocalVariable> VariablesBefore(SyntaxNode statement, SyntaxNode body)
        {
            return statement.ToLocalVariables(filePath).Where(v => v.Location.SpanStart < body.SpanStart).ToImmutableList();
        }

        private ImmutableList<Invocation> InvocationsIn(SyntaxNode node)
        {
            var span = node.Span;

            return invocations.Where(i => span.Contains(i.Location.SpanStart)).ToImmutableList();
        }

        private Statement Plain(StatementKind kind)
        {
            return new Statement { Kind = kind, SourceCode = string.Empty, FilePath = filePath };
        }
    }
}
