using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Anything the parser hands out that stands for a piece of the source: a declaration, a statement,
    /// an expression. Each one knows its text and where it sits, so every finding can quote the code
    /// and point at the line.
    /// </summary>
    /// <remarks>
    /// The body model holds hundreds of thousands of statements and expressions, and nested ones share
    /// their text with the ones around them. Copying the text of each into a string of its own was most
    /// of what building the model cost, while a rule only ever quotes the few it reports. So an element
    /// can keep where its text is and cut it out on first use; to a rule, <see cref="SourceCode"/> reads
    /// the same either way.
    /// </remarks>
    [DebuggerDisplay("{SourceCode}")]
    public abstract record CodeElement
    {
        private string? _sourceCode;

        private readonly SourceSlice _sourceSlice;

        /// <summary>The source text of this element only, not of the whole file.</summary>
        public string SourceCode
        {
            get => _sourceCode ??= _sourceSlice.ToString();
            init => _sourceCode = value;
        }

        public required string FilePath { get; init; }

        /// <summary>Where the element sits in the file. Renders as a clickable <c>path(line,col)</c>.</summary>
        public CodeLocation Location { get; init; } = CodeLocation.None;

        /// <summary>The number of source lines the element spans.</summary>
        public int LineCount => Location.LineCount;

        /// <summary>Sets the text by the node it is cut out of, on first use of <see cref="SourceCode"/>.</summary>
        internal SyntaxNode SourceNode
        {
            init => _sourceSlice = SourceSlice.Of(value);
        }

        /// <summary>Equal when kind, text, file and location are, whether the text was cut out yet or not.</summary>
        public virtual bool Equals(CodeElement? other)
        {
            return other is not null &&
                   (ReferenceEquals(this, other) ||
                    (EqualityContract == other.EqualityContract &&
                     FilePath == other.FilePath &&
                     Location == other.Location &&
                     SourceCode == other.SourceCode));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EqualityContract, FilePath, Location);
        }
    }

    /// <summary>
    /// Where a piece of text sits in the text of its file. The file text is shared by every element of
    /// the file and by <see cref="CSharpSyntaxTree.SourceCode"/>, so keeping it costs nothing extra.
    /// </summary>
    internal readonly struct SourceSlice(SourceText? text, TextSpan span)
    {
        internal static SourceSlice Of(SyntaxNode node)
        {
            return new SourceSlice(node.SyntaxTree.GetText(), node.Span);
        }

        public override string ToString()
        {
            return text?.ToString(span) ?? string.Empty;
        }
    }
}
