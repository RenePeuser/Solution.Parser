using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ExpressionSyntaxExtensions
    {
        /// <summary>Null in, null out, so an optional initializer or default value maps in one line.</summary>
        internal static Expression? ToExpressionOrNull(this ExpressionSyntax? expression, string filePath)
        {
            return expression is null ? null : expression.ToExpression(filePath);
        }

        /// <summary>
        /// The expressions built while one file is parsed, by node. An expression is a tree now, and the
        /// same node is asked for from several sides: as part of a statement, as the argument of a call,
        /// as what is awaited. Each node is built once and the instance shared, as statements are.
        /// </summary>
        [ThreadStatic]
        private static Dictionary<ExpressionSyntax, Expression>? _built;

        /// <summary>The dictionary of the last file, emptied, so the next file on the thread starts at its size.</summary>
        [ThreadStatic]
        private static Dictionary<ExpressionSyntax, Expression>? _spare;

        /// <summary>
        /// Shares the expressions built until the scope is disposed; <see cref="CSharpParser"/> opens one
        /// per file. A nested scope joins the outer one.
        /// </summary>
        internal static Scope ShareExpressions()
        {
            if (_built is not null)
            {
                return new Scope(isOutermost: false);
            }

            _built = _spare ?? new Dictionary<ExpressionSyntax, Expression>();
            _spare = null;

            return new Scope(isOutermost: true);
        }

        internal readonly struct Scope(bool isOutermost) : IDisposable
        {
            public void Dispose()
            {
                if (isOutermost && _built is not null)
                {
                    // Dropping the entries lets the file be collected; the capacity is kept.
                    _built.Clear();
                    _spare = _built;
                    _built = null;
                }
            }
        }

        internal static Expression ToExpression(this ExpressionSyntax expression, string filePath)
        {
            if (_built is null)
            {
                return Build(expression, filePath);
            }

            if (!_built.TryGetValue(expression, out var built))
            {
                built = Build(expression, filePath);
                _built[expression] = built;
            }

            return built;
        }

        private static Expression Build(ExpressionSyntax expression, string filePath)
        {
            var location = expression.ToCodeLocation(filePath);

            // (x) and x! mean x; the kind is that of what is inside.
            var hasNullForgiving = false;
            var inner = expression;

            while (true)
            {
                if (inner is ParenthesizedExpressionSyntax parenthesized)
                {
                    inner = parenthesized.Expression;
                }
                else if (inner is PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed)
                {
                    hasNullForgiving = true;
                    inner = suppressed.Operand;
                }
                else
                {
                    break;
                }
            }

            // a?.b.c is the access c on a.b, made only when a is not null: classify what is accessed
            // and let the bindings inside find a, see ConditionalTarget.
            while (inner is ConditionalAccessExpressionSyntax conditional)
            {
                inner = conditional.WhenNotNull;
            }

            var shape = inner.Classify(filePath);

            return new Expression
            {
                Kind = shape.Kind,
                LiteralValue = shape.LiteralValue,
                InterpolatedParts = shape.InterpolatedParts ?? ImmutableList<InterpolatedStringPart>.Empty,
                TypeReference = shape.TypeReference,
                Operand = shape.Operand,
                Name = shape.Name,
                Operator = shape.Operator,
                Left = shape.Left,
                Right = shape.Right,
                Condition = shape.Condition,
                WhenTrue = shape.WhenTrue,
                WhenFalse = shape.WhenFalse,
                Target = shape.Target,
                Arguments = shape.Arguments ?? ImmutableList<Expression>.Empty,
                Elements = shape.Elements ?? ImmutableList<Expression>.Empty,
                Pattern = shape.Pattern,
                IsPostfix = shape.IsPostfix,
                IsConditional = shape.IsConditional,
                HasNullForgiving = hasNullForgiving,
                SourceNode = expression,
                FilePath = filePath,
                Location = location
            };
        }

        /// <summary>
        /// What <see cref="Classify"/> found out, gathered on the stack, so every expression costs one
        /// allocation rather than one per <c>with</c>.
        /// </summary>
        private readonly record struct Shape(ExpressionKind Kind)
        {
            public object? LiteralValue { get; init; }

            public ImmutableList<InterpolatedStringPart>? InterpolatedParts { get; init; }

            public TypeReference? TypeReference { get; init; }

            public Expression? Operand { get; init; }

            public string? Name { get; init; }

            public string? Operator { get; init; }

            public Expression? Left { get; init; }

            public Expression? Right { get; init; }

            public Expression? Condition { get; init; }

            public Expression? WhenTrue { get; init; }

            public Expression? WhenFalse { get; init; }

            public Expression? Target { get; init; }

            public ImmutableList<Expression>? Arguments { get; init; }

            public ImmutableList<Expression>? Elements { get; init; }

            public Pattern? Pattern { get; init; }

            public bool IsPostfix { get; init; }

            public bool IsConditional { get; init; }
        }

        private static Shape Classify(this ExpressionSyntax expression, string filePath)
        {
            return expression switch
            {
                LiteralExpressionSyntax literal => Of(literal.Kind() switch
                                                      {
                                                          SyntaxKind.StringLiteralExpression or SyntaxKind.Utf8StringLiteralExpression => ExpressionKind.StringLiteral,
                                                          SyntaxKind.NumericLiteralExpression => ExpressionKind.NumericLiteral,
                                                          SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => ExpressionKind.BooleanLiteral,
                                                          SyntaxKind.CharacterLiteralExpression => ExpressionKind.CharLiteral,
                                                          SyntaxKind.NullLiteralExpression => ExpressionKind.NullLiteral,
                                                          SyntaxKind.DefaultLiteralExpression => ExpressionKind.DefaultLiteral,
                                                          _ => ExpressionKind.Other
                                                      }) with
                                                  {
                                                      LiteralValue = literal.IsKind(SyntaxKind.Utf8StringLiteralExpression) || literal.IsKind(SyntaxKind.StringLiteralExpression)
                                                                         ? literal.Token.ValueText
                                                                         : literal.Token.Value
                                                  },

                InterpolatedStringExpressionSyntax interpolated => Of(ExpressionKind.InterpolatedString) with
                {
                    InterpolatedParts = interpolated.Contents.Select(c => c.ToPart(filePath)).ToImmutableList()
                },

                InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } nameOf
                    when nameOf.ArgumentList.Arguments.Count == 1 => Of(ExpressionKind.NameOf) with
                    {
                        LiteralValue = LastName(nameOf.ArgumentList.Arguments[0].Expression)
                    },

                InvocationExpressionSyntax invocation => invocation.Expression.Classify(filePath) switch
                {
                    // The name and receiver of a call are those of what is called: Go in a.Go().
                    { Kind: ExpressionKind.MemberAccess or ExpressionKind.Identifier } callee => Of(ExpressionKind.Invocation) with
                    {
                        Name = callee.Name,
                        Target = callee.Target,
                        IsConditional = callee.IsConditional,
                        Arguments = invocation.ArgumentList.ToArguments(filePath)
                    },

                    // A delegate called right away, as in GetHandler()(x), is called on that expression.
                    _ => Of(ExpressionKind.Invocation) with
                    {
                        Target = invocation.Expression.ToExpression(filePath),
                        Arguments = invocation.ArgumentList.ToArguments(filePath)
                    }
                },

                IdentifierNameSyntax identifier => Of(ExpressionKind.Identifier) with { Name = identifier.Identifier.ValueText },

                GenericNameSyntax generic => Of(ExpressionKind.Identifier) with { Name = generic.Identifier.ValueText },

                MemberAccessExpressionSyntax memberAccess => Of(ExpressionKind.MemberAccess) with
                {
                    Name = memberAccess.Name.Identifier.ValueText,
                    Target = memberAccess.Expression.ToExpression(filePath)
                },

                MemberBindingExpressionSyntax binding => Of(ExpressionKind.MemberAccess) with
                {
                    Name = binding.Name.Identifier.ValueText,
                    Target = ConditionalTarget(binding, filePath),
                    IsConditional = true
                },

                ElementBindingExpressionSyntax elementBinding => Of(ExpressionKind.ElementAccess) with
                {
                    Target = ConditionalTarget(elementBinding, filePath),
                    Arguments = elementBinding.ArgumentList.ToArguments(filePath),
                    IsConditional = true
                },

                ObjectCreationExpressionSyntax creation => Of(ExpressionKind.ObjectCreation) with
                {
                    TypeReference = creation.Type.ToTypeReference(filePath),
                    Arguments = creation.ArgumentList.ToArguments(filePath),
                    Elements = creation.Initializer.ToElements(filePath)
                },

                ImplicitObjectCreationExpressionSyntax implicitCreation => Of(ExpressionKind.ObjectCreation) with
                {
                    Arguments = implicitCreation.ArgumentList.ToArguments(filePath),
                    Elements = implicitCreation.Initializer.ToElements(filePath)
                },

                ArrayCreationExpressionSyntax arrayCreation => Of(ExpressionKind.ArrayCreation) with
                {
                    TypeReference = arrayCreation.Type.ToTypeReference(filePath),
                    Arguments = arrayCreation.Type.RankSpecifiers
                                             .SelectMany(r => r.Sizes)
                                             .Where(s => s is not OmittedArraySizeExpressionSyntax)
                                             .Select(s => s.ToExpression(filePath))
                                             .ToImmutableList(),
                    Elements = arrayCreation.Initializer.ToElements(filePath)
                },

                ImplicitArrayCreationExpressionSyntax implicitArray => Of(ExpressionKind.ArrayCreation) with { Elements = implicitArray.Initializer.ToElements(filePath) },

                StackAllocArrayCreationExpressionSyntax stackAlloc => Of(ExpressionKind.ArrayCreation) with { Elements = stackAlloc.Initializer.ToElements(filePath) },

                ImplicitStackAllocArrayCreationExpressionSyntax implicitStackAlloc => Of(ExpressionKind.ArrayCreation) with { Elements = implicitStackAlloc.Initializer.ToElements(filePath) },

                CollectionExpressionSyntax collection => Of(ExpressionKind.CollectionExpression) with
                {
                    Elements = collection.Elements.Select(e => e.ToElement(filePath)).ToImmutableList()
                },

                AnonymousObjectCreationExpressionSyntax anonymous => Of(ExpressionKind.AnonymousObject) with
                {
                    Elements = anonymous.Initializers.Select(i => i.Expression.ToExpression(filePath)).ToImmutableList()
                },

                AnonymousFunctionExpressionSyntax lambda => Of(ExpressionKind.Lambda) with { Operand = lambda.ExpressionBody.ToExpressionOrNull(filePath) },

                TypeOfExpressionSyntax typeOf => Of(ExpressionKind.TypeOf) with { TypeReference = typeOf.Type.ToTypeReference(filePath) },

                DefaultExpressionSyntax defaultOf => Of(ExpressionKind.Default) with { TypeReference = defaultOf.Type.ToTypeReference(filePath) },

                CastExpressionSyntax cast => Of(ExpressionKind.Cast) with
                {
                    TypeReference = cast.Type.ToTypeReference(filePath),
                    Operand = cast.Expression.ToExpression(filePath)
                },

                BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } asExpression => Of(ExpressionKind.As) with
                {
                    TypeReference = (asExpression.Right as TypeSyntax).ToTypeReference(filePath),
                    Operand = asExpression.Left.ToExpression(filePath),
                    Operator = "as"
                },

                // x is Foo, the old form of the type pattern.
                BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } isType => Of(ExpressionKind.Is) with
                {
                    Operand = isType.Left.ToExpression(filePath),
                    Pattern = isType.Right is TypeSyntax type ? type.ToTypePattern(filePath) : null,
                    Operator = "is"
                },

                IsPatternExpressionSyntax isPattern => Of(ExpressionKind.Is) with
                {
                    Operand = isPattern.Expression.ToExpression(filePath),
                    Pattern = isPattern.Pattern.ToPattern(filePath),
                    Operator = "is"
                },

                BinaryExpressionSyntax binary => Of(ExpressionKind.Binary) with
                {
                    Left = binary.Left.ToExpression(filePath),
                    Right = binary.Right.ToExpression(filePath),
                    Operator = binary.OperatorToken.ValueText
                },

                RangeExpressionSyntax range => Of(ExpressionKind.Range) with
                {
                    Left = range.LeftOperand.ToExpressionOrNull(filePath),
                    Right = range.RightOperand.ToExpressionOrNull(filePath),
                    Operator = ".."
                },

                AwaitExpressionSyntax await => Of(ExpressionKind.Await) with { Operand = await.Expression.ToExpression(filePath) },

                PrefixUnaryExpressionSyntax prefix => Of(ExpressionKind.Unary) with
                {
                    Operand = prefix.Operand.ToExpression(filePath),
                    Operator = prefix.OperatorToken.ValueText
                },

                PostfixUnaryExpressionSyntax postfix => Of(ExpressionKind.Unary) with
                {
                    Operand = postfix.Operand.ToExpression(filePath),
                    Operator = postfix.OperatorToken.ValueText,
                    IsPostfix = true
                },

                ConditionalExpressionSyntax conditional => Of(ExpressionKind.Conditional) with
                {
                    Condition = conditional.Condition.ToExpression(filePath),
                    WhenTrue = conditional.WhenTrue.ToExpression(filePath),
                    WhenFalse = conditional.WhenFalse.ToExpression(filePath)
                },

                AssignmentExpressionSyntax assignment => Of(ExpressionKind.Assignment) with
                {
                    Left = assignment.Left.ToExpression(filePath),
                    Right = assignment.Right.ToExpression(filePath),
                    Operator = assignment.OperatorToken.ValueText
                },

                SwitchExpressionSyntax switchExpression => Of(ExpressionKind.Switch) with
                {
                    Operand = switchExpression.GoverningExpression.ToExpression(filePath),
                    Elements = switchExpression.Arms.Select(a => a.Expression.ToExpression(filePath)).ToImmutableList()
                },

                ThrowExpressionSyntax throwExpression => Of(ExpressionKind.Throw) with { Operand = throwExpression.Expression.ToExpression(filePath) },

                ElementAccessExpressionSyntax elementAccess => Of(ExpressionKind.ElementAccess) with
                {
                    Target = elementAccess.Expression.ToExpression(filePath),
                    Arguments = elementAccess.ArgumentList.ToArguments(filePath)
                },

                TupleExpressionSyntax tuple => Of(ExpressionKind.Tuple) with
                {
                    Elements = tuple.Arguments.Select(a => a.Expression.ToExpression(filePath)).ToImmutableList()
                },

                WithExpressionSyntax with => Of(ExpressionKind.With) with
                {
                    Operand = with.Expression.ToExpression(filePath),
                    Elements = with.Initializer.ToElements(filePath)
                },

                QueryExpressionSyntax => Of(ExpressionKind.Query),

                ThisExpressionSyntax => Of(ExpressionKind.This),

                BaseExpressionSyntax => Of(ExpressionKind.Base),

                PredefinedTypeSyntax predefined => Of(ExpressionKind.Identifier) with { Name = predefined.Keyword.ValueText },

                // checked(x), ref x and the like keep what they wrap.
                CheckedExpressionSyntax checkedExpression => Of(ExpressionKind.Other) with { Operand = checkedExpression.Expression.ToExpression(filePath) },

                RefExpressionSyntax refExpression => Of(ExpressionKind.Other) with { Operand = refExpression.Expression.ToExpression(filePath) },

                _ => Of(ExpressionKind.Other)
            };
        }

        /// <summary>
        /// The expression a <c>?.b</c> or <c>?[0]</c> is applied to: that of the nearest
        /// <c>?.</c> whose right side holds the binding. In <c>a?.b?.c</c> the target of <c>.c</c> is
        /// <c>a?.b</c>, and the target of <c>.b</c> is <c>a</c>.
        /// </summary>
        private static Expression? ConditionalTarget(SyntaxNode binding, string filePath)
        {
            for (var parent = binding.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is ConditionalAccessExpressionSyntax conditional && conditional.WhenNotNull.Span.Contains(binding.Span))
                {
                    return conditional.Expression.ToExpression(filePath);
                }
            }

            return null;
        }

        private static ImmutableList<Expression> ToArguments(this BaseArgumentListSyntax? arguments, string filePath)
        {
            return arguments is null || arguments.Arguments.Count == 0
                       ? ImmutableList<Expression>.Empty
                       : arguments.Arguments.Select(a => a.Expression.ToExpression(filePath)).ToImmutableList();
        }

        /// <summary>The expressions in the braces: <c>X = 1</c> of an object initializer, <c>1</c> of an array initializer.</summary>
        private static ImmutableList<Expression> ToElements(this InitializerExpressionSyntax? initializer, string filePath)
        {
            return initializer is null || initializer.Expressions.Count == 0
                       ? ImmutableList<Expression>.Empty
                       : initializer.Expressions.Select(e => e.ToExpression(filePath)).ToImmutableList();
        }

        private static Expression ToElement(this CollectionElementSyntax element, string filePath)
        {
            return element switch
            {
                ExpressionElementSyntax expression => expression.Expression.ToExpression(filePath),
                SpreadElementSyntax spread => new Expression
                {
                    Kind = ExpressionKind.Spread,
                    Operand = spread.Expression.ToExpression(filePath),
                    Operator = "..",
                    SourceNode = spread,
                    FilePath = filePath,
                    Location = spread.ToCodeLocation(filePath)
                },
                _ => new Expression
                {
                    Kind = ExpressionKind.Other,
                    SourceNode = element,
                    FilePath = filePath,
                    Location = element.ToCodeLocation(filePath)
                }
            };
        }

        private static InterpolatedStringPart ToPart(this InterpolatedStringContentSyntax content, string filePath)
        {
            return content switch
            {
                InterpolationSyntax hole => new InterpolatedStringPart
                {
                    Text = hole.Expression.ToSourceCode(),
                    Hole = hole.Expression.ToExpression(filePath),
                    Format = hole.FormatClause?.FormatStringToken.ValueText
                },
                InterpolatedStringTextSyntax text => new InterpolatedStringPart { Text = Unescape(text) },
                _ => new InterpolatedStringPart { Text = content.ToSourceCode() }
            };
        }

        /// <summary>
        /// The token keeps <c>{{</c> and <c>}}</c> as written. A raw string has no such escapes, its
        /// braces are literal unless they open a hole.
        /// </summary>
        private static string Unescape(InterpolatedStringTextSyntax text)
        {
            var value = text.TextToken.ValueText;
            var isRaw = text.Parent is InterpolatedStringExpressionSyntax interpolated &&
                        interpolated.StringStartToken.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken
                            or SyntaxKind.InterpolatedMultiLineRawStringStartToken;

            return isRaw ? value : value.Replace("{{", "{").Replace("}}", "}");
        }

        /// <summary><c>Bar</c> for <c>Foo.Bar</c>, <c>Foo</c> for <c>Foo</c>, the way <c>nameof</c> reads it.</summary>
        private static string LastName(ExpressionSyntax expression)
        {
            return expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
                SimpleNameSyntax name => name.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => expression.ToSourceCode()
            };
        }

        private static Shape Of(ExpressionKind kind)
        {
            return new Shape(kind);
        }
    }
}
