namespace Solution.Parser.Xaml
{
    /// <summary>The XML namespaces a XAML file uses to mean something other than "a type from a library".</summary>
    public static class XamlNamespaces
    {
        /// <summary>The XAML language namespace, conventionally bound to the <c>x</c> prefix.</summary>
        public const string Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        /// <summary>The WPF presentation namespace, conventionally the default one.</summary>
        public const string Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        /// <summary>The namespace declared by <c>xmlns</c> attributes themselves.</summary>
        public const string Xmlns = "http://www.w3.org/2000/xmlns/";
    }
}
