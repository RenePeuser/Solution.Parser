using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal class ResourceDictionaryRootBuilder : IConcreteRootBuilder
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
            var documentRoot = document.Root;

            var properties = documentRoot.Attributes().Select(attribute => _xamlPropertyParser.ParseFrom(attribute))
                .ToImmutableList();

            var controlBuilder = new ControlsBuilder();
            var controls = controlBuilder.BuildFrom(documentRoot.Descendants().ToImmutableList(), null).ToImmutableList();
            var typeName = documentRoot.Name.LocalName;
            var styles = controls.OfType<Style>().ToImmutableList();
            var dataTemplates = controls.OfType<DataTemplate>().ToImmutableList();
            var fullQualifiedName = xamlFileInfo.FileNameWithoutExtension;

            return new ResourceDictionary(null, null, fullQualifiedName, name, typeName, null, properties, controls,
                styles, dataTemplates);
        }

        public Predicate<string> IsThisTheSelectorFor { get; } = item => item == "ResourceDictionary";
    }
}
