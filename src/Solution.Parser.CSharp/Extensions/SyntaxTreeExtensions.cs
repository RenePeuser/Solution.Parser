using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class SyntaxTreeExtensions
    {
        internal static ImmutableList<T> AllOfType<T>(this SyntaxTree syntaxTree)
            where T : SyntaxNode
        {
            Throw.IfNull(syntaxTree);

            return syntaxTree.GetRoot().DescendantNodes().OfType<T>().ToImmutableList();
        }

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

        /// <summary>
        /// The first namespace of the file, or an empty one when the file declares none. Prefer the
        /// namespace reported on each declaration, which is correct even when a file holds several.
        /// </summary>
        internal static Namespace GetNamespaceOrDefault(this SyntaxTree syntaxTree)
        {
            return syntaxTree.GetNamespaces().FirstOrDefault() ?? new Namespace(string.Empty);
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
