using System.Collections.Immutable;
using System.Xml.Linq;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Carries what every parser in the run needs: where the file is, where to put diagnostics, and a
    /// way back into the value parser for nested markup.
    /// </summary>
    /// <remarks>
    /// Recursion goes through here rather than through constructor injection. The old parsers each
    /// built their own <c>ParserSelector</c> with a hand picked subset of parsers to stop the
    /// constructors recursing forever, which is why a nested extension behaved differently depending
    /// on which parser happened to reach it.
    /// </remarks>
    internal sealed class XamlParseContext
    {
        private readonly ImmutableList<XamlDiagnostic>.Builder _diagnostics = ImmutableList.CreateBuilder<XamlDiagnostic>();
        private readonly PropertyValueParser _valueParser;

        internal XamlParseContext(string filePath, PropertyValueParser valueParser)
        {
            FilePath = filePath;
            _valueParser = valueParser;
        }

        internal string FilePath { get; }

        internal ImmutableList<XamlDiagnostic> Diagnostics => _diagnostics.ToImmutable();

        internal PropertyValue ParseValue(string value, XamlLocation location)
        {
            return _valueParser.Parse(value, location, this);
        }

        internal XamlLocation LocationOf(XObject xObject)
        {
            return xObject.ToXamlLocation(FilePath);
        }

        internal void Report(string id, string message, XamlLocation location)
        {
            _diagnostics.Add(new XamlDiagnostic(id, message, location));
        }
    }
}
