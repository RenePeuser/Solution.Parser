using System;

namespace Solution.Parser.CSharp
{
    internal static class StringLineExtensions
    {
        private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

        /// <summary>
        /// Splits on any line ending rather than on <see cref="Environment.NewLine"/>, so that a file
        /// checked out with the other platform's line endings is read the same way.
        /// </summary>
        internal static string[] SplitLines(this string value)
        {
            return value.Split(LineSeparators, StringSplitOptions.None);
        }
    }
}
