using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class TypeParameterExtensions
    {
        internal static ImmutableList<TypeParameter> ToTypeParameters(this TypeParameterListSyntax? typeParameterList,
                                                                      SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
                                                                      string filePath)
        {
            if (typeParameterList is null)
            {
                return ImmutableList<TypeParameter>.Empty;
            }

            return typeParameterList.Parameters.Select(p => ToTypeParameter(p, constraintClauses, filePath)).ToImmutableList();
        }

        private static TypeParameter ToTypeParameter(TypeParameterSyntax parameter,
                                                     SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
                                                     string filePath)
        {
            var name = parameter.Identifier.ValueText;

            var constraints = constraintClauses.Where(c => c.Name.Identifier.ValueText == name)
                                               .SelectMany(c => c.Constraints)
                                               .Select(c => c.ToString())
                                               .ToImmutableList();

            var variance = parameter.VarianceKeyword.Kind() switch
            {
                SyntaxKind.InKeyword => VarianceKind.In,
                SyntaxKind.OutKeyword => VarianceKind.Out,
                _ => VarianceKind.None
            };

            return new TypeParameter(name, variance, constraints, parameter.AttributeLists.ToAttributes(filePath));
        }
    }
}
