namespace SolutionParser.XAML
{
    internal interface IParserSelector
    {
        IPropertyValueParserBase GetParserFor(string value);
    }
}
