using System;
using System.Linq;

namespace SolutionParser.XAML
{
    internal class SimpleMarkupExtensionParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } = item => item.Contains(':');

        public override PropertyValue Parse(string value, int lineNumber)
        {
            var markupExtension = value[1..^1];
            var bindingInfo = markupExtension.Split(',');

            return new MarkupExtension(value, bindingInfo[0].Split(':').Last());
        }
    }
}
