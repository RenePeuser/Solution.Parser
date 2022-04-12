using System;

namespace SolutionParser.XAML
{
    public class CanNotParseMarkupException : Exception
    {
        public CanNotParseMarkupException(string message) : base(message)
        {
        }
    }
}
