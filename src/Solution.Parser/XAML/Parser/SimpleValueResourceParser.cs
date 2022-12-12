using System;

namespace Solution.Parser.XAML
{
    internal sealed class SimpleValueResourceParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => !item.Contains('{') && !item.Contains(':');

        public override PropertyValue? Parse(string value, int lineNumber)
        {
            return new PropertyValue(value.Trim('\''));
        }
    }
}
