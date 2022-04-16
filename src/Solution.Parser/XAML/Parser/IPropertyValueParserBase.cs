using System;

namespace Solution.Parser.XAML
{
    internal interface IPropertyValueParserBase
    {
        Predicate<string> IsThisTheCorrectParserFor { get; }

        PropertyValue Parse(string value, int lineNumber);
    }
}
