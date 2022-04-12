using System;
using System.Linq;
using System.Xml.Linq;

namespace SolutionParser.XAML
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
                .ToList();

            var controlBuilder = new ControlsBuilder();
            var controls = controlBuilder.BuildFrom(documentRoot.Descendants(), null).ToList();
            var typeName = documentRoot.Name.LocalName;
            var styles = controls.OfType<Style>().ToList();
            var dataTemplates = controls.OfType<DataTemplate>().ToList();
            var fullQualifiedName = xamlFileInfo.FileNameWithoutExtension;

            return new ResourceDictionary(null, null, fullQualifiedName, name, typeName, null, properties, controls,
                styles, dataTemplates);
        }

        public Predicate<string> IsThisTheSelectorFor { get; } = item => item == "ResourceDictionary";
    }
}
