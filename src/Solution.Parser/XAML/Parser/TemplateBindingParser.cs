using System;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal sealed class TemplateBindingParser : PropertyValueParserBase
    {
        private readonly IXamlPropertyValueParser _xamlPropertyValueParser;

        internal TemplateBindingParser() : this(new XamlPropertyValueParser(new ParserSelector(
            new IPropertyValueParserBase[]
            {
                new UrlValueParser(), new SimpleValueResourceParser(), new StaticResourceParser(),
                new XTypeMarkupExtensionParser(), new XStaticMarkupExtensionParser(), new RelativeSourceParser(),
                new StringFormatParser(), new AttachedPropertyParser(), new SimpleMarkupExtensionParser(),
                new XNullParser(),
                // Workaround till all conditions are done
                new UnknownPropertyValueParser()
            })))
        {
        }

        private TemplateBindingParser(IXamlPropertyValueParser xamlPropertyValueParser)
        {
            _xamlPropertyValueParser = xamlPropertyValueParser;
        }

        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => item.StartWith("{TemplateBinding");

        public override PropertyValue? Parse(string value, int lineNumber)
        {
            var newValue = value[..^1];
            var templateBinding = newValue.Replace("{TemplateBinding", string.Empty);
            var result = _xamlPropertyValueParser.Parse(templateBinding, lineNumber);

            return new TemplateBinding(value, result);
        }
    }
}
