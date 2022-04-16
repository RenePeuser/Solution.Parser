using System;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.XAML
{
    public class XamlParser : IXamlParser
    {
        private readonly ControlBuilder _controlBuilder;
        private readonly IRootBuilder _rootBuilder;

        public XamlParser()
            : this(new RootBuilder(), new ControlBuilder())
        {
        }

        private XamlParser(IRootBuilder rootBuilder, ControlBuilder controlBuilder)
        {
            _rootBuilder = rootBuilder;
            _controlBuilder = controlBuilder;
        }

        /// <summary>
        ///     This method reads the content out of the file, access to file system !
        /// </summary>
        public XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo)
        {
            Throw.IfNull(() => xamlFileInfo);

            var fileContent = xamlFileInfo.Value.ReadAllText();

            return Parse(xamlFileInfo, fileContent);
        }

        /// <summary>
        ///     This methods parse the given content and use the file info only as a meta info !
        /// </summary>
        public XamlSyntaxTree Parse(IXamlFileInfo xamlFileInfo, string xamlContent)
        {
            Throw.IfNull(() => xamlFileInfo);

            var document = XDocument.Parse(xamlContent, LoadOptions.SetLineInfo);
            try
            {
                var root = _rootBuilder.BuildFrom(document, xamlFileInfo);
                return new XamlSyntaxTree(xamlFileInfo, document, root);
            }
            catch (Exception e)
            {
                throw new CanNotParseMarkupException(
                    $"Could not parse: {xamlFileInfo.Value.FullName}, because of:\r\n\r\n{e.Message}");
            }
        }

        // <summary>
        /// When you parse an
        /// <see cref="XElement" />
        /// think on it that it is parsed also with line numbers,
        /// otherwise no line number is available.
        /// </summary>
        public ElementBase Parse(XElement element)
        {
            Throw.IfNull(() => element);

            try
            {
                return _controlBuilder.BuildFrom(element, null);
            }
            catch (Exception e)
            {
                throw new CanNotParseMarkupException(
                    $"Could not parse XElement: {element}, because of:\r\n\r\n{e.Message}");
            }
        }
    }
}
