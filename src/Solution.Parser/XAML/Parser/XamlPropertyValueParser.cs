using Argument.Check;

namespace Solution.Parser.XAML
{
    internal sealed class XamlPropertyValueParser : IXamlPropertyValueParser
    {
        private readonly ParserSelector _parserSelector;

        internal XamlPropertyValueParser(ParserSelector parserSelector)
        {
            _parserSelector = parserSelector;
        }

        public PropertyValue? Parse(string value, int lineNumber)
        {
            var parser = _parserSelector.GetParserFor(value);
            return parser.Parse(value, lineNumber);
        }
    }
}
