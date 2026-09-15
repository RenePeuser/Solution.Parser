using System;
using System.IO;
using System.Xml.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    public class XamlParser : IXamlParser
    {
        private readonly ElementBuilder _elementBuilder = new();
        private readonly PropertyValueParser _valueParser = new();

        /// <summary>
        ///     This method reads the content out of the file, access to file system !
        /// </summary>
        public XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo)
        {
            Throw.IfNull(xamlFileInfo);

            return Parse(xamlFileInfo, xamlFileInfo.Value.ReadAllText());
        }

        /// <summary>
        ///     This methods parse the given content and use the file info only as a meta info !
        /// </summary>
        public XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo, string xamlContent)
        {
            Throw.IfNull(xamlFileInfo);

            return ParseContent(xamlContent, xamlFileInfo.Value.FullName) with { FileInfo = xamlFileInfo };
        }

        /// <summary>
        /// Parses markup that is not on disk. <paramref name="filePath"/> is used for
        /// <see cref="XamlLocation"/> and as the fallback name of a root that declares no
        /// <c>x:Class</c>; it does not have to exist.
        /// </summary>
        public XamlSyntaxTree ParseContent(string xamlContent, string filePath)
        {
            Throw.IfNull(xamlContent);

            var document = ToDocument(xamlContent, filePath);
            var documentRoot = Throw.IfNull(document.Root);
            var context = new XamlParseContext(filePath, _valueParser);

            var root = _elementBuilder.BuildRoot(documentRoot, context, FallbackName(filePath));

            return new XamlSyntaxTree
            {
                FilePath = filePath,
                Document = document,
                Root = root,
                Diagnostics = context.Diagnostics
            };
        }

        /// <summary>Parses a single element, for example one pulled out of a document by hand.</summary>
        public ElementBase Parse(XElement element)
        {
            Throw.IfNull(element);

            var context = new XamlParseContext(string.Empty, _valueParser);

            return _elementBuilder.Build(element, context);
        }

        /// <summary>
        /// Malformed XML is the one thing that leaves nothing to parse, so it is the one thing that
        /// still throws. The inner exception is kept; the old parser reduced it to its message.
        /// </summary>
        private static XDocument ToDocument(string xamlContent, string filePath)
        {
            try
            {
                return XDocument.Parse(xamlContent, LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                throw new CanNotParseXamlException($"Could not parse '{filePath}': {e.Message}", e);
            }
        }

        private static string FallbackName(string filePath)
        {
            return filePath.IsNullOrWhiteSpace() ? string.Empty : Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
