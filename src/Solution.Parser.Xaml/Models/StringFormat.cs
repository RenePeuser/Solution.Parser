using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A literal string carrying a composite format item, either from a <c>StringFormat</c> argument
    /// or from an attribute escaped with the <c>{}</c> prefix.
    /// </summary>
    [DebuggerDisplay("{ValueText}")]
    public record StringFormat : PropertyValue
    {
        internal StringFormat(object? value) : base(value)
        {
        }
    }
}
