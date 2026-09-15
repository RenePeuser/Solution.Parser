using System;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The file is not well formed XML, so there is nothing to build a tree from. Anything the parser
    /// can read but not understand becomes a <see cref="XamlDiagnostic"/> instead.
    /// </summary>
    public class CanNotParseXamlException : Exception
    {
        public CanNotParseXamlException(string message) : base(message)
        {
        }

        public CanNotParseXamlException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
