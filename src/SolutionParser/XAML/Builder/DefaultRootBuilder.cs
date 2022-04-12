using System;
using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace SolutionParser.XAML
{
    internal class DefaultRootBuilder : IConcreteRootBuilder
    {
        private readonly IXamlPropertyParser _xamlPropertyParser;

        internal DefaultRootBuilder() : this(new XamlPropertyParser())
        {
        }

        private DefaultRootBuilder(IXamlPropertyParser xamlPropertyParser)
        {
            _xamlPropertyParser = xamlPropertyParser;
        }

        public Root BuildFrom(XDocument document, IXamlFileInfo xamlFileInfo)
        {
            var documentRoot = document.Root;

            var properties = documentRoot.Attributes().Select(attribute => _xamlPropertyParser.ParseFrom(attribute))
                .ToList();

            var controlBuilder = new ControlsBuilder();
            var allElements = documentRoot.Descendants().ToList();
            var controls = controlBuilder.BuildFrom(allElements, null).ToList();

            var typeName = documentRoot.Name.LocalName;
            var xKey = properties.FirstOrDefault(item => item.Name == "Key")?.PropertyValue?.Value.ToString();
            var xName = properties.FirstOrDefault(item => item.Name == "Name")?.PropertyValue?.Value.ToString();

            var styles = controls.OfType<Style>().ToList();
            var dataTemplates = controls.OfType<DataTemplate>().ToList();

            var dataContextProperty = properties.FirstOrDefault(item => item.Name == "DataContext")?.PropertyValue
                ?.Value.ToString();
            var dataContext = dataContextProperty.IsNull() ? null : new DataContext(dataContextProperty);
            var fullQualifiedName = xamlFileInfo.FileNameWithoutExtension;

            return new ControlRoot(dataContext, null, fullQualifiedName, xName, typeName, xKey, properties, controls,
                styles, dataTemplates);
        }

        public Predicate<string> IsThisTheSelectorFor { get; } = item => true;
    }
}
