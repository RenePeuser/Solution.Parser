using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Anything the parser hands out that stands for a piece of the source: a declaration, a statement,
    /// an expression. Each one knows its text and where it sits, so every finding can quote the code
    /// and point at the line.
    /// </summary>
    [DebuggerDisplay("{SourceCode}")]
    public abstract record CodeElement
    {
        /// <summary>The source text of this element only, not of the whole file.</summary>
        public required string SourceCode { get; init; }

        public required string FilePath { get; init; }

        /// <summary>Where the element sits in the file. Renders as a clickable <c>path(line,col)</c>.</summary>
        public CodeLocation Location { get; init; } = CodeLocation.None;

        /// <summary>The number of source lines the element spans.</summary>
        public int LineCount => Location.LineCount;
    }
}
