using System;
using System.Linq;

namespace SolutionParser.XAML
{
    internal class AttachedPropertyParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } = item => item.Contains('.');

        public override PropertyValue Parse(string value, int lineNumber)
        {
            var trimStart = new[] { '{', '(' };
            var trimEnd = new[] { '}', ')' };

            var trimmedValue = value.TrimStart(trimStart).TrimEnd(trimEnd);
            var splittedFullQualifiedName = trimmedValue.Split(':').Last().Split('.');

            return new AttachedPropertyValue(value, splittedFullQualifiedName.First(),
                splittedFullQualifiedName.Last());
        }
    }
}
