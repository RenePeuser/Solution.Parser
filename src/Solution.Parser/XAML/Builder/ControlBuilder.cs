using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal class ControlBuilder : ISpecificControlBuilder
    {
        private readonly IXamlPropertyParser _xamlPropertyParser;

        internal ControlBuilder() : this(new XamlPropertyParser())
        {
        }

        private ControlBuilder(IXamlPropertyParser xamlPropertyParser)
        {
            _xamlPropertyParser = xamlPropertyParser;
        }

        public Predicate<XElement> IsThisTheBuilderFor { get; } = item => true;

        public ElementBase BuildFrom(XElement element, ElementBase parent)
        {
            var properties = element.Attributes().Select(attribute => _xamlPropertyParser.ParseFrom(attribute))
                .ToList();

            var controlBuilder = new ControlsBuilder();
            var allSubElements = element.Descendants().ToList();
            var controls = controlBuilder.BuildFrom(allSubElements, null).ToList();

            var typeName = element.Name.LocalName;
            var xKey = properties.FirstOrDefault(item => item.Name == "Key")?.PropertyValue?.Value.ToString();
            var xName = properties.FirstOrDefault(item => item.Name == "Name")?.PropertyValue?.Value.ToString();

            var styles = controls.OfType<Style>().ToList();
            var dataTemplates = controls.OfType<DataTemplate>().ToList();

            var dataContextProperty = properties.FirstOrDefault(item => item.Name == "DataContext")?.PropertyValue
                ?.Value.ToString();
            var dataContext = dataContextProperty.IsNull() ? null : new DataContext(dataContextProperty);

            var lineNumber = element.Cast<IXmlLineInfo>().LineNumber;

            return new Control(lineNumber, dataContext, parent, xName, typeName, xKey, properties, controls, styles,
                dataTemplates);
        }
    }
}
