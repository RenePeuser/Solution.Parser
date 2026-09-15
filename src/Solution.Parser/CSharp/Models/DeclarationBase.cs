using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Everything the parser reports about a declaration, regardless of its kind.
    /// </summary>
    /// <remarks>
    /// The declaration models use init properties rather than positional parameters on purpose. They
    /// carry a long tail of same typed members, and positional construction is what previously let the
    /// syntax tree and the fully qualified name be passed in swapped order without the compiler
    /// noticing.
    /// </remarks>
    [DebuggerDisplay("{Name}")]
    public abstract record DeclarationBase
    {
        /// <summary>The declared name, without any qualifier.</summary>
        public required string Name { get; init; }

        /// <summary>Namespace, enclosing types and name, separated by dots.</summary>
        public required string FullQualifiedName { get; init; }

        /// <summary>The source text of this declaration only, not of the whole file.</summary>
        public required string SyntaxTree { get; init; }

        public required string FilePath { get; init; }

        /// <summary>Where the declaration sits in the file. Renders as a clickable <c>path(line,col)</c>.</summary>
        public CodeLocation Location { get; init; } = CodeLocation.None;

        /// <summary>The number of source lines the declaration spans.</summary>
        public int LineCount => Location.LineCount;
    }
}
