using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Solution.Parser.CSharp.Models;

namespace Solution.Parser.CSharp
{
    internal static class StatementSyntaxExtensions
    {
        internal static Statement ToStatement(this StatementSyntax structDeclarationSyntax, string filePath)
        {
            return new Statement(structDeclarationSyntax.ToString(), filePath);

        }

        internal static IImmutableList<Statement> ToStatements(this IImmutableList<StatementSyntax> statementSyntaxes, string filePath)
        {
            var statements = ToStatementInternal(statementSyntaxes, filePath).ToImmutableList();
            return statements;

            static IEnumerable<Statement> ToStatementInternal(IImmutableList<StatementSyntax> statementSyntaxes, string filePath)
            {
                foreach (var statementSyntax in statementSyntaxes)
                {
                    switch (statementSyntax)
                    {
                        case BlockSyntax:
                            continue;
                        default:
                            yield return statementSyntax.ToStatement(filePath);
                            break;
                    }
                }
            }
        }
    }
}
