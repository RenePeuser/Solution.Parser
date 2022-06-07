using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Solution.Parser.XAML
{
    internal class ParserSelector : IParserSelector
    {
        private readonly IImmutableList<IPropertyValueParserBase> _propertyValueParser;

        internal ParserSelector(IEnumerable<IPropertyValueParserBase> propertyValueParser)
        {
            _propertyValueParser = propertyValueParser.ToImmutableList();
        }

        public IPropertyValueParserBase GetParserFor(string value)
        {
            var parser = _propertyValueParser.FirstOrDefault(item => item.IsThisTheCorrectParserFor(value));
            return parser;
        }
    }
}
