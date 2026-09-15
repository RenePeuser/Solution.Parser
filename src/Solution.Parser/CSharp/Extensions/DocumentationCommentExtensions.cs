using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class DocumentationCommentExtensions
    {
        internal static DocumentationComment ToDocumentation(this SyntaxNode declaration)
        {
            var trivia = declaration.GetLeadingTrivia()
                                    .Select(t => t.GetStructure())
                                    .OfType<DocumentationCommentTriviaSyntax>()
                                    .FirstOrDefault();

            if (trivia is null)
            {
                return DocumentationComment.None;
            }

            var raw = StripCommentMarkers(trivia.ToString());

            // The fragment has several roots, so it needs a wrapper to be well formed xml. Malformed
            // documentation must not take the whole parse down, hence the fallback below.
            XElement? root;

            try
            {
                root = XElement.Parse($"<doc>{raw}</doc>", LoadOptions.PreserveWhitespace);
            }
            catch (System.Xml.XmlException)
            {
                return DocumentationComment.None with { Raw = raw };
            }

            var parameters = root.Elements("param")
                                 .Select(p => new DocumentedParameter(p.Attribute("name")?.Value ?? string.Empty, Normalize(p.Value)))
                                 .ToImmutableList();

            var exceptions = root.Elements("exception")
                                 .Select(e => e.Attribute("cref")?.Value ?? string.Empty)
                                 .Where(cref => !string.IsNullOrEmpty(cref))
                                 .ToImmutableList();

            return new DocumentationComment(Normalize(root.Element("summary")?.Value),
                                            Normalize(root.Element("remarks")?.Value),
                                            Normalize(root.Element("returns")?.Value),
                                            parameters,
                                            exceptions,
                                            raw);
        }

        private static string StripCommentMarkers(string documentation)
        {
            var lines = documentation.SplitLines()
                                     .Select(line => line.TrimStart())
                                     .Select(line => line.StartsWith("///", StringComparison.Ordinal) ? line[3..] : line);

            return string.Join(Environment.NewLine, lines).Trim();
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var lines = value.SplitLines().Select(line => line.Trim()).Where(line => line.Length > 0);

            return string.Join(" ", lines);
        }
    }
}
