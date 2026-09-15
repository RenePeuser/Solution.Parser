using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The document element of a XAML file. <see cref="Window"/>, <see cref="UserControl"/>,
    /// <see cref="Page"/>, <see cref="Application"/>, <see cref="ResourceDictionaryRoot"/> and
    /// <see cref="ControlRoot"/> are siblings below it, so <c>is</c> checks mean what they say. A
    /// window is not a user control, which is what the old hierarchy claimed.
    /// </summary>
    [DebuggerDisplay("{FullTypeName,nq} {FullQualifiedName}")]
    public abstract record Root : ElementBase
    {
        public override ElementKind Kind => ElementKind.Root;

        /// <summary>Distinguishes the document element flavours.</summary>
        public abstract RootKind RootKind { get; }

        /// <summary>
        /// The value of <c>x:Class</c>, which names the code behind type. Falls back to the file name
        /// for a document element that declares no class, such as a resource dictionary.
        /// </summary>
        public string FullQualifiedName { get; init; } = string.Empty;

        /// <summary>The <c>xmlns</c> declarations on the document element, prefix and all.</summary>
        public ImmutableList<XamlUsing> XmlnsDeclarations { get; init; } = ImmutableList<XamlUsing>.Empty;
    }
}
