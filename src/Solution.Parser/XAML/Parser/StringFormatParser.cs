using System;

namespace Solution.Parser.XAML
{
    internal sealed class StringFormatParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => item.Contains("{}") || item.Contains("{0}");

        public override PropertyValue? Parse(string value, int lineNumber)
        {
            return new StringFormat(value.Trim('\''));
        }
    }
}
