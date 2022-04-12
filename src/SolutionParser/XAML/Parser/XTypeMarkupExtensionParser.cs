using System;
using System.Linq;
using Extensions.Pack;

namespace SolutionParser.XAML
{
    internal class XTypeMarkupExtensionParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } = item => item.StartWith("{x:Type");

        public override PropertyValue Parse(string value, int lineNumber)
        {
            var result = value.TrimStart('{').TrimEnd('}');
            var splittedInfo = result.Split(' ');
            var typeInfo = splittedInfo.Last().Split(':').Last();


            return new XTypeMarkupExtension(typeInfo);
        }
    }
}
