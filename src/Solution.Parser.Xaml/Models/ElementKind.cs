namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Distinguishes the element shapes the parser recognises, so a rule can switch on the kind
    /// instead of comparing <see cref="ElementBase.TypeName"/> against string literals.
    /// </summary>
    public enum ElementKind
    {
        /// <summary>Any element the parser has no special shape for, which is most of them.</summary>
        Control,

        /// <summary>The document element. See <see cref="Root.RootKind"/> for which one.</summary>
        Root,

        Style,

        Setter,

        /// <summary><c>Trigger</c>, <c>DataTrigger</c>, <c>MultiTrigger</c>, <c>EventTrigger</c> and friends.</summary>
        Trigger,

        /// <summary><c>DataTemplate</c>, <c>ControlTemplate</c>, <c>ItemsPanelTemplate</c>, <c>HierarchicalDataTemplate</c>.</summary>
        Template,

        ResourceDictionary
    }
}
