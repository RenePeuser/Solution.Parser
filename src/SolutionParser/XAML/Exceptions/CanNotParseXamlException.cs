using System;

namespace SolutionParser.XAML
{
    public class CanNotParseXamlException : Exception
    {
        public CanNotParseXamlException(string message) : base(message)
        {
        }
    }
}
