using System;

namespace Solution.Parser.XAML
{
    internal class XNullParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => item.ToUpperInvariant().Contains("x:Null".ToUpperInvariant());

        public override PropertyValue Parse(string value, int lineNumber)
        {
            return new PropertyValue(null);
        }
    }
}
