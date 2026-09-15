using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FilePath}")]
    public record XamlSyntaxTree
    {
        public required string FilePath { get; init; }

        public required Root Root { get; init; }

        /// <summary>The underlying XML, for anything the model does not cover.</summary>
        public required XDocument Document { get; init; }

        /// <summary>
        /// What the parser could not make sense of. A single unreadable attribute does not stop the
        /// parse, so a rule can assert this is empty instead of losing the file to an exception.
        /// </summary>
        public ImmutableList<XamlDiagnostic> Diagnostics { get; init; } = ImmutableList<XamlDiagnostic>.Empty;

        /// <summary>The file this tree was read from, null when the content was parsed from memory.</summary>
        public IXamlFileInfo? FileInfo { get; init; }

        public bool HasDiagnostics => !Diagnostics.IsEmpty;

        /// <summary>The <c>xmlns</c> declarations on the document element.</summary>
        public ImmutableList<XamlUsing> XmlnsDeclarations => Root.XmlnsDeclarations;

        /// <summary>The value of <c>x:Class</c>, which names the code behind type.</summary>
        public string FullQualifiedName => Root.FullQualifiedName;

        public XamlUsing? Xmlns(string alias)
        {
            return XmlnsDeclarations.FirstOrDefault(x => x.Alias == alias);
        }
    }
}
