namespace Solution.Parser.Xaml
{
    /// <summary>Which document element a XAML file starts with.</summary>
    public enum RootKind
    {
        /// <summary>A document element the parser has no special shape for.</summary>
        Control,

        Window,

        UserControl,

        Page,

        ResourceDictionary,

        Application
    }
}
