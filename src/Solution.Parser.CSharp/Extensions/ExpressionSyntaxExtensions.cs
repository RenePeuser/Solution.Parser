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

        internal static Expression ToExpression(this ExpressionSyntax expression, string filePath)
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

            var shape = inner.Classify(filePath);

            return new Expression
            {
                Kind = shape.Kind,
                LiteralValue = shape.LiteralValue,
                InterpolatedParts = shape.InterpolatedParts ?? ImmutableList<InterpolatedStringPart>.Empty,
                TypeReference = shape.TypeReference,
                Operand = shape.Operand,
                Name = shape.Name,
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

                InvocationExpressionSyntax => Of(ExpressionKind.Invocation),

                IdentifierNameSyntax identifier => Of(ExpressionKind.Identifier) with { Name = identifier.Identifier.ValueText },

                GenericNameSyntax generic => Of(ExpressionKind.Identifier) with { Name = generic.Identifier.ValueText },

                MemberAccessExpressionSyntax memberAccess => Of(ExpressionKind.MemberAccess) with { Name = memberAccess.Name.Identifier.ValueText },

                ConditionalAccessExpressionSyntax conditional => conditional.WhenNotNull is InvocationExpressionSyntax
                                                                     ? Of(ExpressionKind.Invocation)
                                                                     : Of(ExpressionKind.MemberAccess) with { Name = LastName(conditional.WhenNotNull) },

                ObjectCreationExpressionSyntax creation => Of(ExpressionKind.ObjectCreation) with { TypeReference = creation.Type.ToTypeReference(filePath) },

                ImplicitObjectCreationExpressionSyntax => Of(ExpressionKind.ObjectCreation),

                ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax
                    or StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax => Of(ExpressionKind.ArrayCreation),

                CollectionExpressionSyntax => Of(ExpressionKind.CollectionExpression),

                AnonymousObjectCreationExpressionSyntax => Of(ExpressionKind.AnonymousObject),

                AnonymousFunctionExpressionSyntax => Of(ExpressionKind.Lambda),

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
                    Operand = asExpression.Left.ToExpression(filePath)
                },

                BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } or IsPatternExpressionSyntax => Of(ExpressionKind.Is),

                BinaryExpressionSyntax => Of(ExpressionKind.Binary),

                AwaitExpressionSyntax await => Of(ExpressionKind.Await) with { Operand = await.Expression.ToExpression(filePath) },

                PrefixUnaryExpressionSyntax prefix => Of(ExpressionKind.Unary) with { Operand = prefix.Operand.ToExpression(filePath) },

                PostfixUnaryExpressionSyntax postfix => Of(ExpressionKind.Unary) with { Operand = postfix.Operand.ToExpression(filePath) },

                ConditionalExpressionSyntax => Of(ExpressionKind.Conditional),

                AssignmentExpressionSyntax => Of(ExpressionKind.Assignment),

                SwitchExpressionSyntax => Of(ExpressionKind.Switch),

                ThrowExpressionSyntax => Of(ExpressionKind.Throw),

                ElementAccessExpressionSyntax => Of(ExpressionKind.ElementAccess),

                TupleExpressionSyntax => Of(ExpressionKind.Tuple),

                WithExpressionSyntax => Of(ExpressionKind.With),

                ThisExpressionSyntax => Of(ExpressionKind.This),

                BaseExpressionSyntax => Of(ExpressionKind.Base),

                PredefinedTypeSyntax predefined => Of(ExpressionKind.Identifier) with { Name = predefined.Keyword.ValueText },

                _ => Of(ExpressionKind.Other)
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
