using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The shared shape of every element in a XAML file. <see cref="Control"/>, <see cref="Style"/>,
    /// <see cref="Setter"/>, <see cref="Trigger"/>, <see cref="Template"/>,
    /// <see cref="ResourceDictionaryElement"/> and the <see cref="Root"/> flavours are siblings below
    /// it, so <c>is</c> checks and pattern matching mean what they say.
    /// </summary>
    /// <remarks>
    /// <see cref="Children"/> holds the elements declared directly inside this one, nothing deeper.
    /// Use the query extensions (<c>AllElements()</c>, <c>DescendantElements()</c>, <c>AllStyles()</c>)
    /// to walk the whole tree.
    /// </remarks>
    [DebuggerDisplay("{FullTypeName,nq} Name:{XName} Key:{XKey}")]
    public abstract record ElementBase
    {
        /// <summary>The element name without its prefix, for example <c>Button</c> for <c>&lt;local:Button /&gt;</c>.</summary>
        public string TypeName { get; init; } = string.Empty;

        public XamlLocation Location { get; init; } = XamlLocation.None;

        /// <summary>Distinguishes the element shapes the parser recognises.</summary>
        public abstract ElementKind Kind { get; }

        /// <summary>The XML namespace URI the element name resolves to, empty when the document declares none.</summary>
        public string XmlNamespace { get; init; } = string.Empty;

        /// <summary>The prefix as written in the file, for example <c>local</c> for <c>&lt;local:Button /&gt;</c>.</summary>
        public string Prefix { get; init; } = string.Empty;

        /// <summary>The value of <c>x:Name</c>, falling back to the <c>Name</c> property when there is no directive.</summary>
        public string XName { get; init; } = string.Empty;

        /// <summary>The value of <c>x:Key</c>. Empty for anything not declared in a resource dictionary.</summary>
        public string XKey { get; init; } = string.Empty;

        /// <summary>
        /// The text written directly inside the element, as in <c>&lt;Button&gt;Click me&lt;/Button&gt;</c>.
        /// Null when the element has no text of its own.
        /// </summary>
        public string? Content { get; init; }

        /// <summary>The <c>DataContext</c> set on this element, null when it does not set one.</summary>
        public DataContext? DataContext { get; init; }

        /// <summary>The element this one is declared in, null only on the <see cref="Root"/>.</summary>
        public ElementBase? Parent { get; internal set; }

        /// <summary>
        /// Everything set on this element: attributes, attached properties and property elements such
        /// as <c>&lt;Grid.RowDefinitions&gt;</c>, which XAML treats as properties rather than children.
        /// </summary>
        public ImmutableList<Property> Properties { get; init; } = ImmutableList<Property>.Empty;

        /// <summary>The elements declared directly inside this one. Property elements are not in here.</summary>
        public ImmutableList<ElementBase> Children { get; init; } = ImmutableList<ElementBase>.Empty;

        /// <summary>The element name as written, prefix included.</summary>
        public string FullTypeName => Prefix.IsNullOrEmpty() ? TypeName : $"{Prefix}:{TypeName}";

        /// <summary>The property elements among <see cref="Properties"/>.</summary>
        public ImmutableList<PropertyElement> PropertyElements => Properties.OfType<PropertyElement>().ToImmutableList();

        /// <summary>The attached properties among <see cref="Properties"/>.</summary>
        public ImmutableList<AttachedProperty> AttachedProperties => Properties.OfType<AttachedProperty>().ToImmutableList();

        /// <summary>
        /// What the <c>&lt;X.Resources&gt;</c> property element declares, flattened through a nested
        /// <c>&lt;ResourceDictionary&gt;</c> so that both spellings read the same.
        /// </summary>
        public ImmutableList<ElementBase> Resources
        {
            get
            {
                var declared = PropertyElements.FirstOrDefault(p => p.Name.EqualsTo("Resources"))?.Children
                               ?? ImmutableList<ElementBase>.Empty;

                return declared.Count.EqualsTo(1) && declared[0] is ResourceDictionaryElement dictionary
                    ? dictionary.Children
                    : declared;
            }
        }

        /// <summary>
        /// Finds a property by name. A name containing a dot is matched against the full qualified name
        /// of an attached property or a property element, so <c>element["Grid.Row"]</c> works.
        /// </summary>
        public Property? this[string name] => FindPropertyByName(name);

        private Property? FindPropertyByName(string name)
        {
            if (!name.Contains('.'))
            {
                return Properties.FirstOrDefault(p => p.Name.EqualsTo(name));
            }

            return Properties.OfType<AttachedProperty>().FirstOrDefault(p => p.FullQualifiedName.EqualsTo(name))
                   ?? (Property?)Properties.OfType<PropertyElement>().FirstOrDefault(p => p.FullQualifiedName.EqualsTo(name));
        }
    }
}
