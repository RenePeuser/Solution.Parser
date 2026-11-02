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
    /// Shared extraction of the executable part of a member, so a method, a local function, a
    /// constructor, an operator and a finalizer all report their body the same way.
    /// </summary>
    internal readonly record struct MemberBody(string Text,
                                               ImmutableList<string> Statements,
                                               ImmutableList<string> LineStatements,
                                               ImmutableList<LocalFunction> LocalFunctions,
                                               ImmutableList<Invocation> Invocations,
                                               ImmutableList<LocalVariable> LocalVariables,
                                               bool IsExpressionBodied,
                                               bool IsIterator)
    {
        public ImmutableList<ObjectCreation> ObjectCreations { get; init; } = ImmutableList<ObjectCreation>.Empty;

        public ImmutableList<StringLiteral> StringLiterals { get; init; } = ImmutableList<StringLiteral>.Empty;

        public ImmutableList<MemberAccess> MemberAccesses { get; init; } = ImmutableList<MemberAccess>.Empty;

        public ImmutableList<Statement> BodyStatements { get; init; } = ImmutableList<Statement>.Empty;

        public ImmutableList<Statement> AllStatements { get; init; } = ImmutableList<Statement>.Empty;

        public ImmutableList<SwitchExpression> SwitchExpressions { get; init; } = ImmutableList<SwitchExpression>.Empty;

        public ImmutableList<Lambda> Lambdas { get; init; } = ImmutableList<Lambda>.Empty;

        public ImmutableList<Expression> Awaits { get; init; } = ImmutableList<Expression>.Empty;
    }

    internal static class BodyExtensions
    {
        internal static MemberBody ToMemberBody(this BlockSyntax? block, ArrowExpressionClauseSyntax? expressionBody, string filePath)
        {
            if (block is not null)
            {
                var text = block.ToSourceCode();

                var scan = BodyScan.Of(block);
                var invocations = scan.Invocations.ToInvocations(filePath);

                return new MemberBody(text,
                                      block.Statements.Select(s => s.ToSourceCode()).ToImmutableList(),
                                      ToLineStatements(text),
                                      scan.LocalFunctions.Select(l => l.ToLocalFunction(filePath)).ToImmutableList(),
                                      invocations,
                                      scan.Variables.Select(v => v.ToLocalVariableOrNull(filePath, invocations)).OfType<LocalVariable>().ToImmutableList(),
                                      IsExpressionBodied: false,
                                      IsIterator: scan.HasYield).WithElementsOf(block, scan, filePath);
            }

            if (expressionBody is not null)
            {
                var text = expressionBody.Expression.ToSourceCode();

                var scan = BodyScan.Of(expressionBody.Expression);
                var invocations = scan.Invocations.ToInvocations(filePath);

                return new MemberBody(text,
                                      ImmutableList.Create(text),
                                      ToLineStatements(text),
                                      ImmutableList<LocalFunction>.Empty,
                                      invocations,
                                      scan.Variables.Select(v => v.ToLocalVariableOrNull(filePath, invocations)).OfType<LocalVariable>().ToImmutableList(),
                                      IsExpressionBodied: true,
                                      IsIterator: false).WithElementsOf(expressionBody.Expression, scan, filePath);
            }

            return new MemberBody(string.Empty,
                                  ImmutableList<string>.Empty,
                                  ImmutableList<string>.Empty,
                                  ImmutableList<LocalFunction>.Empty,
                                  ImmutableList<Invocation>.Empty,
                                  ImmutableList<LocalVariable>.Empty,
                                  IsExpressionBodied: false,
                                  IsIterator: false);
        }

        private static MemberBody WithElementsOf(this MemberBody body, SyntaxNode node, BodyScan scan, string filePath)
        {
            var builder = new StatementBuilder(filePath, body.Invocations, body.LocalVariables);

            return body with
            {
                ObjectCreations = scan.ObjectCreations.Select(c => c.ToObjectCreation(filePath)).ToImmutableList(),
                StringLiterals = scan.Strings.Select(s => s.ToStringLiteral(filePath)).ToImmutableList(),
                MemberAccesses = scan.MemberAccesses.Select(a => a.ToMemberAccess(filePath)).ToImmutableList(),
                BodyStatements = node is BlockSyntax block ? builder.Build(block.Statements) : ImmutableList<Statement>.Empty,
                AllStatements = builder.Build(scan.Statements),
                SwitchExpressions = scan.SwitchExpressions.Select(builder.BuildSwitchExpression).ToImmutableList(),
                Lambdas = scan.Lambdas.Select(builder.BuildLambda).ToImmutableList(),
                Awaits = scan.Awaits.Select(a => a.ToExpression(filePath)).ToImmutableList()
            };
        }

        /// <summary>
        /// The local functions of the block, including those nested inside another local function.
        /// </summary>
        internal static ImmutableList<LocalFunction> ToLocalFunctions(this SyntaxNode node, string filePath)
        {
            return node.DescendantNodes()
                       .OfType<LocalFunctionStatementSyntax>()
                       .Select(l => l.ToLocalFunction(filePath))
                       .ToImmutableList();
        }

        /// <summary>
        /// The source lines of the body without the enclosing braces and without blank lines. Split on
        /// any line ending rather than on the platform one, so a file with the other platform's line
        /// endings is read the same way.
        /// </summary>
        private static ImmutableList<string> ToLineStatements(string body)
        {
            // Lines as ranges first, so only the lines that are kept become strings.
            var lines = new List<Range>();
            var start = 0;

            while (true)
            {
                var end = body.AsSpan(start).IndexOfAny('\r', '\n');

                if (end < 0)
                {
                    lines.Add(start..body.Length);

                    break;
                }

                lines.Add(start..(start + end));
                start += end + (body[start + end] == '\r' && start + end + 1 < body.Length && body[start + end + 1] == '\n' ? 2 : 1);
            }

            var first = lines.Count > 0 && body.AsSpan()[lines[0]].Trim().SequenceEqual("{") ? 1 : 0;
            var last = lines.Count > first && body.AsSpan()[lines[^1]].Trim().SequenceEqual("}") ? lines.Count - 1 : lines.Count;

            var result = ImmutableList.CreateBuilder<string>();

            for (var i = first; i < last; i++)
            {
                if (!body.AsSpan()[lines[i]].IsWhiteSpace())
                {
                    result.Add(body[lines[i]]);
                }
            }

            return result.ToImmutable();
        }
    }
}
