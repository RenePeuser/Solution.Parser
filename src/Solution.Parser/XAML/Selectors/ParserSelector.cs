using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;

namespace Solution.Parser.XAML
{
    internal sealed class ParserSelector : IParserSelector
    {
        private readonly IImmutableList<IPropertyValueParserBase> _propertyValueParser;

        internal ParserSelector(IEnumerable<IPropertyValueParserBase> propertyValueParser)
        {
            _propertyValueParser = propertyValueParser.ToImmutableList();
        }

        public IPropertyValueParserBase GetParserFor(string value)
        {
            var parser = _propertyValueParser.FirstOrDefault(item => item.IsThisTheCorrectParserFor(value));
            Throw.If(parser, item => item == null, $"No parser was found for property value: {value}");

            return parser!;
        }
    }
}
