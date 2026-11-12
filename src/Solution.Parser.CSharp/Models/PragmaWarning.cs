using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A <c>#pragma warning disable</c> or <c>#pragma warning restore</c> directive, wherever it sits in
    /// the file. The parser reports only what is written; which lines a pragma covers is for the rule to
    /// decide.
    /// </summary>
    /// <example>
    /// <code>
    /// from pragma in tree.PragmaWarnings
    /// where pragma.Action == PragmaWarningAction.Disable &amp;&amp; string.IsNullOrWhiteSpace(pragma.Comment)
    /// select $"{pragma.Location}: #pragma warning disable needs a reason";
    /// </code>
    /// </example>
    [DebuggerDisplay("#pragma warning {Action} {string.Join(\", \", Ids),nq}")]
    public record PragmaWarning(PragmaWarningAction Action, ImmutableList<string> Ids, string? Comment, CodeLocation Location)
    {
        /// <summary>
        /// True when no id is written, as in <c>#pragma warning disable</c>, which switches off every warning.
        /// </summary>
        public bool AppliesToAllWarnings => Ids.IsEmpty;
    }

    /// <summary>What a <see cref="PragmaWarning"/> does to the warnings it names.</summary>
    public enum PragmaWarningAction
    {
        Disable,
        Restore
    }
}
