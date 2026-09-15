using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Where a declaration sits in its file. Lines and columns are one based, matching what an editor
    /// shows. <see cref="ToString"/> renders the MSBuild style form, which test runners and IDEs turn
    /// into a clickable link, so a failing code rule can point straight at the offending line.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public record CodeLocation(string FilePath,
                               int StartLine,
                               int StartColumn,
                               int EndLine,
                               int EndColumn,
                               int SpanStart,
                               int SpanLength)
    {
        public static CodeLocation None { get; } = new(string.Empty, 0, 0, 0, 0, 0, 0);

        public int LineCount => EndLine - StartLine + 1;

        public override string ToString()
        {
            return $"{FilePath}({StartLine},{StartColumn})";
        }
    }

    internal static class CodeLocationExtensions
    {
        internal static CodeLocation ToCodeLocation(this SyntaxNode node, string filePath)
        {
            var span = node.GetLocation().GetLineSpan().Span;

            return new CodeLocation(filePath,
                                    span.Start.Line + 1,
                                    span.Start.Character + 1,
                                    span.End.Line + 1,
                                    span.End.Character + 1,
                                    node.Span.Start,
                                    node.Span.Length);
        }
    }
}
