using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// A <c>&lt;ResourceDictionary&gt;</c> that is not the document element, for example the one inside
    /// <c>&lt;UserControl.Resources&gt;</c> or an entry in <c>MergedDictionaries</c>.
    /// </summary>
    [DebuggerDisplay("{FullTypeName,nq} Key:{XKey}")]
    public record ResourceDictionaryElement : ElementBase
    {
        public override ElementKind Kind => ElementKind.ResourceDictionary;

        /// <summary>The value of <c>Source</c> for a dictionary pulled in from another file, empty otherwise.</summary>
        public string Source => this["Source"]?.PropertyValue?.ValueText ?? string.Empty;
    }
}
