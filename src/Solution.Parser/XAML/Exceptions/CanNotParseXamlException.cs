using System;

namespace Solution.Parser.XAML
{
    public class CanNotParseXamlException : Exception
    {
        public CanNotParseXamlException(string message) : base(message)
        {
        }
    }
}
