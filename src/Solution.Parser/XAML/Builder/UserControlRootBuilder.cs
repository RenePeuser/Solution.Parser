using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal sealed class UserControlRootBuilder : IConcreteRootBuilder
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
            var documentRoot = Throw.IfNull(document.Root);

            var properties = documentRoot.Attributes().Select(attribute => _xamlPropertyParser.ParseFrom(attribute))
                                         .ToImmutableList();

            var controlBuilder = new ControlsBuilder();
            var allElements = documentRoot.Descendants().ToImmutableList();
            var controls = controlBuilder.BuildFrom(allElements, null).ToImmutableList();

            var typeName = documentRoot.Name.LocalName;
            var xKey = properties.FirstOrDefault(item => item.Name == "Key")?.PropertyValue?.Value?.ToString() ?? string.Empty;
            var xName = properties.FirstOrDefault(item => item.Name == "Name")?.PropertyValue?.Value?.ToString() ?? string.Empty;


            var styles = controls.OfType<Style>().ToImmutableList();
            var dataTemplates = controls.OfType<DataTemplate>().ToImmutableList();

            var dataContextProperty = properties.FirstOrDefault(item => item.Name == "DataContext");
            var dataContextValue = dataContextProperty?.PropertyValue?.As<MarkupExtension>()?["Type"]?.PropertyValue?.ValueText ?? string.Empty;

            var dataContext = dataContextProperty.IsNull() ? null : new DataContext(dataContextValue);

            var fullQualifiedName = properties.FirstOrDefault(p => p.Name == "Class")?.PropertyValue?.ValueText ?? string.Empty;

            return new UserControl(dataContext, null, fullQualifiedName, xName, typeName, xKey, properties, controls,
                styles, dataTemplates);
        }

        public Predicate<string> IsThisTheSelectorFor { get; } = item => item == "UserControl";
    }
}
