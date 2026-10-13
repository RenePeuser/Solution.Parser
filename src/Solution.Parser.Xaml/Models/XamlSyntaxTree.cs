using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{FilePath}")]
    public record XamlSyntaxTree
    {
        /// <summary>Reads and parses the XAML file at <paramref name="path"/>.</summary>
        public static XamlSyntaxTree Parse(string path)
        {
            Throw.IfNullOrWhiteSpace(path);

            return Parse(new FileInfo(path));
        }

        /// <summary>Reads and parses the given XAML file.</summary>
        public static XamlSyntaxTree Parse(FileInfo fileInfo)
        {
            return new XamlParser().Parse(fileInfo);
        }

        /// <summary>Parses <paramref name="xamlContent"/>; the file only names where it came from.</summary>
        public static XamlSyntaxTree Parse(FileInfo fileInfo, string xamlContent)
        {
            return new XamlParser().Parse(fileInfo, xamlContent);
        }

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
        public FileInfo? FileInfo { get; init; }

        public bool HasDiagnostics => !Diagnostics.IsEmpty;

        /// <summary>The <c>xmlns</c> declarations on the document element.</summary>
        public ImmutableList<XamlUsing> XmlnsDeclarations => Root.XmlnsDeclarations;

        /// <summary>The value of <c>x:Class</c>, which names the code behind type.</summary>
        public string FullyQualifiedName => Root.FullyQualifiedName;

        public XamlUsing? Xmlns(string alias)
        {
            return XmlnsDeclarations.FirstOrDefault(x => x.Alias == alias);
        }
    }
}
