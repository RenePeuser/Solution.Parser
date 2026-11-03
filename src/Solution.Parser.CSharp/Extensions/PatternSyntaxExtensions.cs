using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class PatternSyntaxExtensions
    {
        internal static Pattern ToPattern(this PatternSyntax pattern, string filePath)
        {
            // (p) means p.
            while (pattern is ParenthesizedPatternSyntax parenthesized)
            {
                pattern = parenthesized.Pattern;
            }

            var seed = new Pattern
            {
                Kind = PatternKind.Other,
                SourceNode = pattern,
                FilePath = filePath,
                Location = pattern.ToCodeLocation(filePath)
            };

            return pattern switch
            {
                ConstantPatternSyntax constant => seed with { Kind = PatternKind.Constant, Value = constant.Expression.ToExpression(filePath) },

                TypePatternSyntax type => seed with { Kind = PatternKind.Type, TypeReference = type.Type.ToTypeReference(filePath) },

                DeclarationPatternSyntax declaration => seed with
                {
                    Kind = PatternKind.Declaration,
                    TypeReference = declaration.Type.ToTypeReference(filePath),
                    Designation = declaration.Designation.ToName()
                },

                VarPatternSyntax var => seed with { Kind = PatternKind.Var, Designation = var.Designation.ToName() },

                DiscardPatternSyntax => seed with { Kind = PatternKind.Discard },

                RelationalPatternSyntax relational => seed with
                {
                    Kind = PatternKind.Relational,
                    Operator = relational.OperatorToken.ValueText,
                    Value = relational.Expression.ToExpression(filePath)
                },

                UnaryPatternSyntax not => seed with { Kind = PatternKind.Not, Operand = not.Pattern.ToPattern(filePath) },

                BinaryPatternSyntax binary => seed with
                {
                    Kind = binary.OperatorToken.ValueText == "and" ? PatternKind.And : PatternKind.Or,
                    Operator = binary.OperatorToken.ValueText,
                    Left = binary.Left.ToPattern(filePath),
                    Right = binary.Right.ToPattern(filePath)
                },

                RecursivePatternSyntax recursive => seed with
                {
                    Kind = PatternKind.Recursive,
                    TypeReference = recursive.Type?.ToTypeReference(filePath),
                    Designation = recursive.Designation?.ToName(),
                    Positional = recursive.PositionalPatternClause?.Subpatterns.Select(s => s.Pattern.ToPattern(filePath)).ToImmutableList() ?? ImmutableList<Pattern>.Empty,
                    Properties = recursive.PropertyPatternClause?.Subpatterns
                                          .Select(s => new PropertyPattern
                                          {
                                              Name = s.ExpressionColon?.Expression.ToSourceCode() ?? string.Empty,
                                              Pattern = s.Pattern.ToPattern(filePath)
                                          })
                                          .ToImmutableList() ?? ImmutableList<PropertyPattern>.Empty,
                    IsPropertyClause = recursive.PropertyPatternClause is not null
                },

                ListPatternSyntax list => seed with
                {
                    Kind = PatternKind.List,
                    Designation = list.Designation?.ToName(),
                    Elements = list.Patterns.Select(p => p.ToPattern(filePath)).ToImmutableList()
                },

                SlicePatternSyntax slice => seed with { Kind = PatternKind.Slice, Operand = slice.Pattern?.ToPattern(filePath) },

                _ => seed
            };
        }

        /// <summary>The pattern of the old <c>x is Foo</c>, which names a type without the pattern syntax.</summary>
        internal static Pattern ToTypePattern(this TypeSyntax type, string filePath)
        {
            return new Pattern
            {
                Kind = PatternKind.Type,
                TypeReference = type.ToTypeReference(filePath),
                SourceNode = type,
                FilePath = filePath,
                Location = type.ToCodeLocation(filePath)
            };
        }

        /// <summary><c>x</c> for <c>Foo x</c>, null for a discard, the source for <c>var (a, b)</c>.</summary>
        private static string? ToName(this VariableDesignationSyntax designation)
        {
            return designation switch
            {
                SingleVariableDesignationSyntax single => single.Identifier.ValueText,
                DiscardDesignationSyntax => null,
                _ => designation.ToSourceCode()
            };
        }
    }
}
