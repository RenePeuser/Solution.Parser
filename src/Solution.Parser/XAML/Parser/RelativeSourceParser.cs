using System;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal class RelativeSourceParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => item.StartWith("{RelativeSource");

        public override PropertyValue Parse(string value, int lineNumber)
        {
            return new RelativeSource(value.Split(' ').Last().TrimEnd('}'));
        }
    }
}
