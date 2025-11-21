using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal sealed class ResourceDictionaryBuilder : ISpecificControlBuilder
    {
        private readonly IXamlPropertyParser _xamlPropertyParser;

        internal ResourceDictionaryBuilder() : this(new XamlPropertyParser())
        {
        }

        private ResourceDictionaryBuilder(IXamlPropertyParser xamlPropertyParser)
        {
            _xamlPropertyParser = xamlPropertyParser;
        }

        public Predicate<XElement> IsThisTheBuilderFor { get; } = item => item.Name.LocalName.EqualsTo("ResourceDictionary");

        public ElementBase BuildFrom(XElement element, ElementBase? parent)
        {
            var name = element.Name.LocalName;

            var properties = element.Attributes().Select(attribute => _xamlPropertyParser.ParseFrom(attribute)!)
                .ToImmutableList();

            var controlBuilder = new ControlsBuilder();
            var controls = controlBuilder.BuildFrom(element.Descendants().ToImmutableList(), null).ToImmutableList();

            var typeName = element.Name.LocalName;
            var key = properties.FirstOrDefault(item => item.Name.EqualsTo("Key"))?.PropertyValue?.Value?.ToString() ?? string.Empty;

            var styles = controls.OfType<Style>().ToImmutableList();
            var dataTemplates = controls.OfType<DataTemplate>().ToImmutableList();

            var dataContextProperty = properties.FirstOrDefault(item => item.Name.EqualsTo("DataContext"))?.PropertyValue?.Value?.ToString() ?? string.Empty;
            var dataContext = dataContextProperty.IsNull() ? null : new DataContext(dataContextProperty);

            var lineNumber = element.Cast<IXmlLineInfo>().LineNumber;

            return new ResourceDictionaryControl(lineNumber, dataContext, parent, name, typeName, key, properties,
                controls, styles, dataTemplates);
        }
    }
}
