using System;

namespace Solution.Parser.XAML
{
    public class CanNotParseMarkupException : Exception
    {
        public CanNotParseMarkupException(string message) : base(message)
        {
        }
    }
}
