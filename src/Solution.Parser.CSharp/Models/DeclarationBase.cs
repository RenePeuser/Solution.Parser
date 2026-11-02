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
    public abstract record DeclarationBase : CodeElement
    {
        /// <summary>The declared name, without any qualifier.</summary>
        public required string Name { get; init; }

        /// <summary>Namespace, enclosing types and name, separated by dots.</summary>
        public required string FullyQualifiedName { get; init; }
    }
}
