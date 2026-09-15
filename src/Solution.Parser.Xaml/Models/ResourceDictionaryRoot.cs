using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>A file whose document element is <c>&lt;ResourceDictionary&gt;</c>.</summary>
    [DebuggerDisplay("{FullQualifiedName}")]
    public record ResourceDictionaryRoot : Root
    {
        public override RootKind RootKind => RootKind.ResourceDictionary;
    }
}
