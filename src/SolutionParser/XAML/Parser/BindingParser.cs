using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Pack;

namespace SolutionParser.XAML
{
    internal class BindingParser : PropertyValueParserBase
    {
        private readonly IXamlPropertyValueParser _xamlPropertyValueParser;

        internal BindingParser() : this(new XamlPropertyValueParser(new ParserSelector(new IPropertyValueParserBase[]
        {
            new XNullParser(),
            new UrlValueParser(),
            new SimpleValueResourceParser(),
            new StaticResourceParser(),
            new XTypeMarkupExtensionParser(),
            new XStaticMarkupExtensionParser(),
            new RelativeSourceParser(),
            new StringFormatParser(),
            new AttachedPropertyParser(),
            new SimpleMarkupExtensionParser(),
            // Workaround till all conditions are done            
            new UnknownPropertyValueParser()
        })))
        {
        }

        private BindingParser(IXamlPropertyValueParser xamlPropertyValueParser)
        {
            _xamlPropertyValueParser = xamlPropertyValueParser;
        }

        public override Predicate<string> IsThisTheCorrectParserFor { get; } = item => item.StartWith("{Binding");

        public override PropertyValue Parse(string value, int lineNumber)
        {
            var newValue = value.Substring(0, value.Length - 1);
            var binding = newValue.Replace("{Binding", string.Empty);
            var bindingInfo = binding.Split(',');

            // Path only
            var correctedBindingInfo = bindingInfo.FirstOrDefault(item => !item.Contains('='));
            if (correctedBindingInfo.IsNotNull())
            {
                var correctPath = correctedBindingInfo.IsNullOrWhiteSpace() ? "Path=." : $"Path={correctedBindingInfo}";
                bindingInfo[bindingInfo.IndexOf(correctedBindingInfo)] = correctPath;
            }

            // Path missing
            var pathIsMissing = bindingInfo.None(item => item.Contains("Path"));
            if (pathIsMissing)
            {
                bindingInfo = bindingInfo.Concat("Path=.").ToArray();
            }


            var dictionary = bindingInfo[0].IsNotNullOrEmpty()
                ? bindingInfo.Select(item => item.Split('='))
                    .ToDictionary(item => item[0].Trim(), item => item[1].Trim())
                : new Dictionary<string, string>();

            var path = dictionary.ContainsKey("Path")
                ? _xamlPropertyValueParser.Parse(dictionary["Path"], lineNumber)
                : null;
            var source = dictionary.ContainsKey("Source")
                ? _xamlPropertyValueParser.Parse(dictionary["Source"], lineNumber)
                : null;
            var converter = dictionary.ContainsKey("Converter")
                ? _xamlPropertyValueParser.Parse(dictionary["Converter"], lineNumber)
                : null;
            var converterParameter = dictionary.ContainsKey("ConverterParameter")
                ? _xamlPropertyValueParser.Parse(dictionary["ConverterParameter"], lineNumber)
                : null;
            var mode = dictionary.ContainsKey("Mode")
                ? _xamlPropertyValueParser.Parse(dictionary["Mode"], lineNumber).Value.ToString()
                : "Default";
            var updateSourceTrigger = dictionary.ContainsKey("UpdateSourceTrigger")
                ? _xamlPropertyValueParser.Parse(dictionary["UpdateSourceTrigger"], lineNumber).Value.ToString()
                : "Default";
            var elementName = dictionary.ContainsKey("ElementName")
                ? _xamlPropertyValueParser.Parse(dictionary["ElementName"], lineNumber).Value.ToString()
                : null;
            var fallbackValue = dictionary.ContainsKey("FallbackValue")
                ? _xamlPropertyValueParser.Parse(dictionary["FallbackValue"], lineNumber)
                : null;
            var relativeSource = dictionary.ContainsKey("RelativeSource")
                ? _xamlPropertyValueParser.Parse(dictionary["RelativeSource"], lineNumber)
                : null;
            var stringFormat = dictionary.ContainsKey("StringFormat")
                ? _xamlPropertyValueParser.Parse(dictionary["StringFormat"], lineNumber)
                : null;

            return new Binding(value, path, source, converter, converterParameter, mode, updateSourceTrigger,
                elementName, relativeSource as RelativeSource, fallbackValue, stringFormat as StringFormat);
        }
    }
}
