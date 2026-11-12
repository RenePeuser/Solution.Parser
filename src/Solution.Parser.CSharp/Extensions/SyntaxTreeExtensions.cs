using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class SyntaxTreeExtensions
    {
        /// <summary>Every namespace declared in the file, in source order.</summary>
        internal static ImmutableList<Namespace> GetNamespaces(this SyntaxTree syntaxTree)
        {
            Throw.IfNull(syntaxTree);

            // A namespace only sits in the file or in another namespace; no need to walk every method body.
            return syntaxTree.GetRoot()
                             .DescendantNodes(descendIntoChildren: n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                             .OfType<BaseNamespaceDeclarationSyntax>()
                             .Select(ns => new Namespace(ns.Name.ToSourceCode()))
                             .ToImmutableList();
        }

        internal static ImmutableList<ParseDiagnostic> ToParseDiagnostics(this SyntaxTree syntaxTree, string filePath)
        {
            return syntaxTree.GetDiagnostics()
                             .Select(d => new ParseDiagnostic(d.Id,
                                                              d.Severity,
                                                              d.GetMessage(),
                                                              ToLocation(d, filePath)))
                             .ToImmutableList();
        }

        /// <summary>Every <c>#pragma warning</c> of the file, in source order, those inside types and bodies included.</summary>
        internal static ImmutableList<PragmaWarning> ToPragmaWarnings(this SyntaxTree syntaxTree, string filePath)
        {
            var pragmas = ImmutableList.CreateBuilder<PragmaWarning>();

            // The directive chain skips every subtree without a directive, instead of walking all the trivia.
            for (var directive = syntaxTree.GetRoot().GetFirstDirective(IsPragmaWarning);
                 directive is not null;
                 directive = directive.GetNextDirective(IsPragmaWarning))
            {
                var pragma = (PragmaWarningDirectiveTriviaSyntax)directive;

                pragmas.Add(new PragmaWarning(pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword)
                                                  ? PragmaWarningAction.Disable
                                                  : PragmaWarningAction.Restore,
                                              pragma.ErrorCodes.Where(c => !c.IsMissing).Select(ToWarningId).ToImmutableList(),
                                              TrailingComment(pragma),
                                              pragma.ToCodeLocation(filePath)));
            }

            return pragmas.ToImmutable();
        }

        private static bool IsPragmaWarning(DirectiveTriviaSyntax directive)
        {
            return directive is PragmaWarningDirectiveTriviaSyntax;
        }

        /// <summary>
        /// The id as the compiler reads it: <c>CA2007</c> stays as written, a bare number like <c>618</c>
        /// is a compiler warning and becomes <c>CS0618</c>, so a rule matches it whichever way it is written.
        /// </summary>
        private static string ToWarningId(ExpressionSyntax errorCode)
        {
            return errorCode is LiteralExpressionSyntax { Token.Value: int number }
                       ? $"CS{number:0000}"
                       : errorCode.ToString().Trim();
        }

        /// <summary>
        /// The <c>//</c> comment written on the directive's line, without the slashes; null when there is
        /// none. A directive allows no <c>/* */</c> comment, Roslyn reports CS1072 for it.
        /// </summary>
        private static string? TrailingComment(PragmaWarningDirectiveTriviaSyntax pragma)
        {
            var comment = pragma.DescendantTrivia()
                                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));

            return comment.IsKind(SyntaxKind.SingleLineCommentTrivia)
                       ? comment.ToString()[2..].Trim()
                       : null;
        }

        private static CodeLocation ToLocation(Diagnostic diagnostic, string filePath)
        {
            var span = diagnostic.Location.GetLineSpan().Span;
            var textSpan = diagnostic.Location.SourceSpan;

            return new CodeLocation(filePath,
                                    span.Start.Line + 1,
                                    span.Start.Character + 1,
                                    span.End.Line + 1,
                                    span.End.Character + 1,
                                    textSpan.Start,
                                    textSpan.Length);
        }
    }
}
