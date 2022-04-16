using System;

namespace Solution.Parser.XAML
{
    internal class UnknownPropertyValueParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } = item => true;

        public override PropertyValue Parse(string value, int lineNumber)
        {
            return new UnknownPropertyValue(value);
        }
    }
}
