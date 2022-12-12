using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.XAML
{
    internal sealed class ResourceDictionaryRootBuilder : IConcreteRootBuilder
    {
        private readonly IXamlPropertyParser _xamlPropertyParser;

        internal ResourceDictionaryRootBuilder() : this(new XamlPropertyParser())
        {
        }

        private ResourceDictionaryRootBuilder(IXamlPropertyParser xamlPropertyParser)
        {
            _xamlPropertyParser = xamlPropertyParser;
        }

        public Root BuildFrom(XDocument document, IXamlFileInfo xamlFileInfo)
        {
            var name = xamlFileInfo.FileNameWithoutExtension;
            var documentRoot = Throw.IfNull(document.Root);

            var properties = documentRoot.Attributes().Select(attribute => _xamlPropertyParser.ParseFrom(attribute)!).ToImmutableList();

            var controlBuilder = new ControlsBuilder();
            var controls = controlBuilder.BuildFrom(documentRoot.Descendants().ToImmutableList(), null).ToImmutableList();
            var typeName = documentRoot.Name.LocalName;
            var styles = controls.OfType<Style>().ToImmutableList();
            var dataTemplates = controls.OfType<DataTemplate>().ToImmutableList();
            var fullQualifiedName = xamlFileInfo.FileNameWithoutExtension;

            return new ResourceDictionary(null, null, fullQualifiedName, name, typeName, string.Empty, properties, controls,
                styles, dataTemplates);
        }

        public Predicate<string> IsThisTheSelectorFor { get; } = item => item == "ResourceDictionary";
    }
}
