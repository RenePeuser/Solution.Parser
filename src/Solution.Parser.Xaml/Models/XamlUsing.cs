using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// An <c>xmlns</c> declaration. <see cref="Alias"/> is the prefix it binds, which is what maps a
    /// <c>local:</c> in the markup back to a CLR namespace. The old parser left it empty because the
    /// prefix was dropped before the value ever reached here.
    /// </summary>
    [DebuggerDisplay("{Alias}={Namespace}")]
    public record XamlUsing : PropertyValue
    {
        internal XamlUsing(string rawValue) : base(rawValue)
        {
        }

        /// <summary>The prefix this declaration binds, empty for the default <c>xmlns</c>.</summary>
        public required string Alias { get; init; }

        /// <summary>The CLR namespace for a <c>clr-namespace:</c> declaration, otherwise the raw URI.</summary>
        public required string Namespace { get; init; }

        /// <summary>The assembly named by <c>;assembly=</c>, empty when the declaration names none.</summary>
        public string Assembly { get; init; } = string.Empty;

        /// <summary>True when the declaration points at a CLR namespace rather than at a schema URI.</summary>
        public bool IsClrNamespace { get; init; }
    }
}
