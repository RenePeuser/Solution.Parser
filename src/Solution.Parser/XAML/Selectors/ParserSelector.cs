using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal sealed class ParserSelector : IParserSelector
    {
        private readonly ImmutableList<IPropertyValueParserBase> _propertyValueParser = ImmutableList<IPropertyValueParserBase>.Empty;

        internal ParserSelector(IEnumerable<IPropertyValueParserBase> propertyValueParser)
        {
            _propertyValueParser = propertyValueParser.ToImmutableList();
        }

        public IPropertyValueParserBase GetParserFor(string value)
        {
            var parser = _propertyValueParser.FirstOrDefault(item => item.IsThisTheCorrectParserFor(value));
            Throw.If(parser, item => item.IsNull(), $"No parser was found for property value: {value}");

            return parser!;
        }
    }
}
