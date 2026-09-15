using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class StatementSyntaxExtensions
    {
        /// <summary>
        /// The top level statements of the file, in source order. A file that declares types only has
        /// none; statements inside a member belong to that member.
        /// </summary>
        internal static ImmutableList<Statement> ToTopLevelStatements(this CompilationUnitSyntax root, string filePath)
        {
            return root.Members
                       .OfType<GlobalStatementSyntax>()
                       .Select(g => new Statement(g.Statement.ToString(), filePath, g.ToCodeLocation(filePath)))
                       .ToImmutableList();
        }
    }
}
