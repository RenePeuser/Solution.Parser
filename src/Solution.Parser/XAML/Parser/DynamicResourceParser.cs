using System;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal class DynamicResourceParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => item.StartWith("{DynamicResource");

        public override PropertyValue Parse(string value, int lineNumber)
        {
            return new DynamicResource(value.Split(' ').Last().TrimEnd('}'));
        }
    }
}
