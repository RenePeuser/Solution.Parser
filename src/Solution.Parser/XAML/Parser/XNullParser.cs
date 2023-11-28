using System;

namespace Solution.Parser.XAML
{
    internal sealed class XNullParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => item.ToUpperInvariant().Contains("x:Null".ToUpperInvariant(), StringComparison.InvariantCulture);

        public override PropertyValue? Parse(string value, int lineNumber)
        {
            return new PropertyValue(null);
        }
    }
}
