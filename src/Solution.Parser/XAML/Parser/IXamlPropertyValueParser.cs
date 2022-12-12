namespace Solution.Parser.XAML
{
    internal interface IXamlPropertyValueParser
    {
        PropertyValue? Parse(string value, int lineNumber);
    }
}
