using System;

namespace Solution.Parser.Xaml
{
    public class CanNotParseMarkupException : Exception
    {
        public CanNotParseMarkupException(string message) : base(message)
        {
        }

        public CanNotParseMarkupException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
