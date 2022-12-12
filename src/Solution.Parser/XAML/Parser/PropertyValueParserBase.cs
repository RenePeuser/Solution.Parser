using System;

namespace Solution.Parser.XAML
{
    internal abstract class PropertyValueParserBase : IPropertyValueParserBase
    {
        public abstract Predicate<string> IsThisTheCorrectParserFor { get; }

        public abstract PropertyValue? Parse(string value, int lineNumber);
    }
}
