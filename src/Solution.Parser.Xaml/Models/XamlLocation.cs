using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Where an element or a property sits in its file. Lines and columns are one based, matching what
    /// an editor shows. <see cref="ToString"/> renders the MSBuild style form, which test runners and
    /// IDEs turn into a clickable link, so a failing code rule can point straight at the offending line.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public record XamlLocation(string FilePath, int Line, int Column)
    {
        public static XamlLocation None { get; } = new(string.Empty, 0, 0);

        public override string ToString()
        {
            return $"{FilePath}({Line},{Column})";
        }
    }

    internal static class XamlLocationExtensions
    {
        /// <summary>
        /// Line info is only present when the document was loaded with <see cref="LoadOptions.SetLineInfo"/>,
        /// so a document built in memory falls back to the file path alone instead of reporting line 0
        /// as if it were real.
        /// </summary>
        internal static XamlLocation ToXamlLocation(this XObject xObject, string filePath)
        {
            if (xObject is not IXmlLineInfo lineInfo || !lineInfo.HasLineInfo())
            {
                return new XamlLocation(filePath, 0, 0);
            }

            return new XamlLocation(filePath, lineInfo.LineNumber, lineInfo.LinePosition);
        }
    }
}
