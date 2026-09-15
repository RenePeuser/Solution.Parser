using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A value the parser recognised as markup but could not break down further. The raw text is
    /// preserved, and the parse records a <see cref="XamlDiagnostic"/> alongside it.
    /// </summary>
    [DebuggerDisplay("{ValueText}")]
    public record UnknownPropertyValue : PropertyValue
    {
        internal UnknownPropertyValue(string value) : base(value)
        {
        }
    }
}
