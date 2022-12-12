using System;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal sealed class StaticResourceParser : PropertyValueParserBase
    {
        private static readonly string SearchPattern = "{StaticResource";

        public override Predicate<string> IsThisTheCorrectParserFor { get; } = item => item.StartWith(SearchPattern);

        public override PropertyValue? Parse(string value, int lineNumber)
        {
            return new StaticResource(value.Replace(SearchPattern, string.Empty).TrimEnd('}').Trim());
        }
    }
}
