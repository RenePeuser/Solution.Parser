namespace Solution.Parser.XAML
{
    internal interface IParserSelector
    {
        IPropertyValueParserBase GetParserFor(string value);
    }
}
