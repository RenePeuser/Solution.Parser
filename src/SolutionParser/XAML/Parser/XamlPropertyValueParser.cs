using Argument.Check;

namespace SolutionParser.XAML
{
    internal class XamlPropertyValueParser : IXamlPropertyValueParser
    {
        private readonly ParserSelector _parserSelector;

        internal XamlPropertyValueParser(ParserSelector parserSelector)
        {
            _parserSelector = parserSelector;
        }

        public PropertyValue Parse(string value, int lineNumber)
        {
            var parser = _parserSelector.GetParserFor(value);
            Throw.If(() => parser, item => item == null, $"No parser was found for property value: {value}");

            return parser.Parse(value, lineNumber);
        }
    }
}
