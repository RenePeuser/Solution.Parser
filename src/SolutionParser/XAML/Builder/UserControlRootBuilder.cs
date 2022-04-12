using System;
using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace SolutionParser.XAML
{
    internal class UserControlRootBuilder : IConcreteRootBuilder
    {
        private readonly IXamlPropertyParser _xamlPropertyParser;

        internal UserControlRootBuilder() : this(new XamlPropertyParser())
        {
        }

        private UserControlRootBuilder(IXamlPropertyParser xamlPropertyParser)
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

            var dataContextProperty = properties.FirstOrDefault(item => item.Name == "DataContext");
            var dataContextValue = dataContextProperty?.PropertyValue?.As<MarkupExtension>()?["Type"]?.PropertyValue
                ?.ValueText;

            var dataContext = dataContextProperty.IsNull() ? null : new DataContext(dataContextValue);

            var fullQualifiedName = properties.FirstOrDefault(p => p.Name == "Class")?.PropertyValue?.ValueText;

            return new UserControl(dataContext, null, fullQualifiedName, xName, typeName, xKey, properties, controls,
                styles, dataTemplates);
        }

        public Predicate<string> IsThisTheSelectorFor { get; } = item => item == "UserControl";
    }
}
