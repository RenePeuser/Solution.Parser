using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    public class XamlPropertyParser : IXamlPropertyParser
    {
        private readonly IXamlPropertyValueParser _xalXamlPropertyValueParser;

        public XamlPropertyParser() : this(new XamlPropertyValueParser(new ParserSelector(
            new IPropertyValueParserBase[]
            {
                new XamlUsingParser(), new UrlValueParser(), new SimpleValueResourceParser(),
                new StaticResourceParser(), new DynamicResourceParser(), new XTypeMarkupExtensionParser(),
                new XStaticMarkupExtensionParser(), new RelativeSourceParser(), new BindingParser(),
                new MarkupExtensionParser(), new XNullParser(), new AttachedPropertyParser(),
                new TemplateBindingParser(), new StringFormatParser(),
                // Workaround till all conditions are done
                new UnknownPropertyValueParser()
            })))
        {
        }

        private XamlPropertyParser(IXamlPropertyValueParser xalXamlPropertyValueParser)
        {
            _xalXamlPropertyValueParser = xalXamlPropertyValueParser;
        }

        public Property ParseFrom(XAttribute attribute)
        {
            var lineNumber = attribute.Cast<IXmlLineInfo>().LineNumber;
            var attributeName = attribute.Name.LocalName.Split('.');
            var propertyValue = _xalXamlPropertyValueParser.Parse(attribute.Value, lineNumber);

            if (attributeName.Length == 1)
            {
                return new Property(lineNumber, attributeName.First(), propertyValue);
            }

            return new AttachedProperty(lineNumber, attributeName.First(), attributeName.Last() + "Property",
                attributeName.Last(), propertyValue);
        }
    }
}
