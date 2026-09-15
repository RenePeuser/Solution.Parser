using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>One argument of a markup extension. <see cref="Name"/> is null for a positional argument.</summary>
    [DebuggerDisplay("{Name,nq}={Value}")]
    internal sealed record MarkupArgument(string? Name, string Value);

    /// <summary>
    /// A markup extension broken into its name and arguments, with nesting, quoting and escaping
    /// already resolved. Every markup parser works off this instead of splitting the raw text again,
    /// which is where the old parser lost <c>StringFormat={0:#,##0.00}</c> to its comma.
    /// </summary>
    [DebuggerDisplay("{FullName,nq} ({Arguments.Count} arguments)")]
    internal sealed record MarkupToken(string RawValue,
                                       string Prefix,
                                       string Name,
                                       ImmutableList<MarkupArgument> Arguments)
    {
        internal string FullName => Prefix.IsNullOrEmpty() ? Name : $"{Prefix}:{Name}";

        internal ImmutableList<string> PositionalArguments =>
            Arguments.Where(a => a.Name is null).Select(a => a.Value).ToImmutableList();

        /// <summary>The first positional argument, empty when the extension has none.</summary>
        internal string FirstPositionalArgument => PositionalArguments.FirstOrDefault() ?? string.Empty;

        /// <summary>The value of a named argument, null when the extension does not set it.</summary>
        internal string? this[string name] => Arguments.FirstOrDefault(a => a.Name is not null && a.Name.EqualsTo(name))?.Value;
    }
}
