using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Extensions.Pack;

namespace SolutionParser.XAML
{
    internal class ResourceDictionaryBuilder : ISpecificControlBuilder
    {
        private readonly IXamlPropertyParser _xamlPropertyParser;

        internal ResourceDictionaryBuilder() : this(new XamlPropertyParser())
        {
        }

        private ResourceDictionaryBuilder(IXamlPropertyParser xamlPropertyParser)
        {
            _xamlPropertyParser = xamlPropertyParser;
        }

        public Predicate<XElement> IsThisTheBuilderFor { get; } = item => item.Name.LocalName == "ResourceDictionary";

        public ElementBase BuildFrom(XElement element, ElementBase parent)
        {
            var name = element.Name.LocalName;

            var properties = element.Attributes().Select(attribute => _xamlPropertyParser.ParseFrom(attribute))
                .ToList();

            var controlBuilder = new ControlsBuilder();
            var controls = controlBuilder.BuildFrom(element.Descendants(), null).ToList();

            var typeName = element.Name.LocalName;
            var key = properties.FirstOrDefault(item => item.Name == "Key")?.PropertyValue?.Value.ToString();

            var styles = controls.OfType<Style>().ToList();
            var dataTemplates = controls.OfType<DataTemplate>().ToList();

            var dataContextProperty = properties.FirstOrDefault(item => item.Name == "DataContext")?.PropertyValue
                ?.Value.ToString();
            var dataContext = dataContextProperty.IsNull() ? null : new DataContext(dataContextProperty);

            var lineNumber = element.Cast<IXmlLineInfo>().LineNumber;

            return new ResourceDictionaryControl(lineNumber, dataContext, parent, name, typeName, key, properties,
                controls, styles, dataTemplates);
        }
    }
}
