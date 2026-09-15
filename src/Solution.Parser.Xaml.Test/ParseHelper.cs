using Solution.Parser.Xaml;

namespace Solution.Parser.Xaml.Test
{
    internal static class ParseHelper
    {
        internal const string FilePath = @"C:\repo\Sample.xaml";

        private const string Namespaces =
            """
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:local="clr-namespace:My.Sample;assembly=My.Sample"
            """;

        /// <summary>Parses markup as written, for a test that cares about the document element itself.</summary>
        internal static XamlSyntaxTree ParseXaml(string xaml)
        {
            return xaml.ParseXaml(FilePath);
        }

        /// <summary>Wraps a fragment in a user control that declares the usual namespaces.</summary>
        internal static XamlSyntaxTree ParseFragment(string fragment)
        {
            return ParseXaml($"<UserControl {Namespaces}>{fragment}</UserControl>");
        }
    }
}
