using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The XML documentation comment of a declaration. <see cref="None"/> stands for an undocumented
    /// declaration, so a rule such as "every public member is documented" reads as
    /// <c>member.Documentation.IsDocumented.IsFalse()</c>.
    /// </summary>
    [DebuggerDisplay("{Summary}")]
    public record DocumentationComment(string Summary,
                                       string Remarks,
                                       string Returns,
                                       ImmutableList<DocumentedParameter> Parameters,
                                       ImmutableList<string> Exceptions,
                                       string Raw)
    {
        public static DocumentationComment None { get; } = new(string.Empty,
                                                               string.Empty,
                                                               string.Empty,
                                                               ImmutableList<DocumentedParameter>.Empty,
                                                               ImmutableList<string>.Empty,
                                                               string.Empty);

        public bool IsDocumented => !string.IsNullOrWhiteSpace(Raw);

        public bool HasSummary => !string.IsNullOrWhiteSpace(Summary);

        /// <summary>An <c>&lt;inheritdoc /&gt;</c> comment carries no text of its own.</summary>
        public bool IsInheritDoc => Raw.Contains("<inheritdoc", System.StringComparison.OrdinalIgnoreCase);
    }

    [DebuggerDisplay("{Name}")]
    public record DocumentedParameter(string Name, string Description);
}
