using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Something the parser could not make sense of. A single unreadable attribute does not stop the
    /// parse, so a rule asserting that no file has any diagnostic lets malformed markup surface
    /// instead of quietly losing the whole file.
    /// </summary>
    [DebuggerDisplay("{Id} {Message}")]
    public record XamlDiagnostic(string Id, string Message, XamlLocation Location)
    {
        /// <summary>A markup extension that could not be tokenised, for example one with unbalanced braces.</summary>
        public const string MalformedMarkupExtension = "XAML0001";

        /// <summary>A markup extension whose arguments did not have the expected shape.</summary>
        public const string UnexpectedMarkupArgument = "XAML0002";
    }
}
