namespace SolutionParser.XAML
{
    internal interface IXamlPropertyValueParser
    {
        PropertyValue Parse(string value, int lineNumber);
    }
}
