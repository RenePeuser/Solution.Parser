using System.Collections.Generic;
using System.Linq;

namespace SolutionParser.XAML
{
    internal class ParserSelector : IParserSelector
    {
        private readonly IEnumerable<IPropertyValueParserBase> _propertyValueParser;

        internal ParserSelector(IEnumerable<IPropertyValueParserBase> propertyValueParser)
        {
            _propertyValueParser = propertyValueParser.ToList();
        }

        public IPropertyValueParserBase GetParserFor(string value)
        {
            var parser = _propertyValueParser.FirstOrDefault(item => item.IsThisTheCorrectParserFor(value));
            return parser;
        }
    }
}
