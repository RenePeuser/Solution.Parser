using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Builds the element tree.
    /// </summary>
    /// <remarks>
    /// One recursion over <see cref="XContainer.Elements()"/>, so every element is built exactly once
    /// and ends up under the element it is written in. The seven builders this replaces each iterated
    /// <c>Descendants()</c> and then recursed into every descendant again, which both flattened the
    /// tree and made the work exponential in its depth: a sixteen level file produced 131.072 nodes
    /// for seventeen elements.
    /// </remarks>
    internal sealed class ElementBuilder
    {
        private readonly XamlPropertyParser _propertyParser = new();

        internal ElementBase Build(XElement element, XamlParseContext context)
        {
            return Apply(CreateEmpty(element.Name.LocalName), ToParts(element, context));
        }

        internal Root BuildRoot(XElement element, XamlParseContext context, string fallbackName)
        {
            var parts = ToParts(element, context);
            var root = (Root)Apply(CreateEmptyRoot(element.Name.LocalName), parts);

            var xmlnsDeclarations = element.Attributes()
                                           .Where(a => a.IsNamespaceDeclaration)
                                           .Select(a => _propertyParser.ToXamlUsing(a))
                                           .ToImmutableList();

            var declaredClass = parts.Properties.FirstOrDefault(p => p.IsXamlDirective && p.Name.EqualsTo("Class"))
                                     ?.PropertyValue?.ValueText;

            var withRootParts = root with
            {
                FullQualifiedName = declaredClass.IsNullOrWhiteSpace() ? fallbackName : declaredClass!,
                XmlnsDeclarations = xmlnsDeclarations
            };

            Reparent(withRootParts);

            return withRootParts;
        }

        private ElementParts ToParts(XElement element, XamlParseContext context)
        {
            var properties = element.Attributes()
                                    .Where(a => !a.IsNamespaceDeclaration)
                                    .Select(a => _propertyParser.ParseFrom(a, context))
                                    .Concat(element.Elements().Where(IsPropertyElement).Select(e => ToPropertyElement(e, context)))
                                    .ToImmutableList();

            var children = element.Elements()
                                  .Where(e => !IsPropertyElement(e))
                                  .Select(e => Build(e, context))
                                  .ToImmutableList();

            var parts = new ElementParts
            {
                TypeName = element.Name.LocalName,
                Prefix = element.GetPrefixOfNamespace(element.Name.Namespace) ?? string.Empty,
                XmlNamespace = element.Name.NamespaceName,
                Location = context.LocationOf(element),
                XName = NameOf(properties),
                XKey = properties.FirstOrDefault(p => p.IsXamlDirective && p.Name.EqualsTo("Key"))?.PropertyValue?.ValueText ?? string.Empty,
                Content = ContentOf(element),
                DataContext = ToDataContext(properties.FirstOrDefault(p => !p.IsXamlDirective && p.Name.EqualsTo("DataContext"))),
                Properties = properties,
                Children = children
            };

            return parts;
        }

        /// <summary>
        /// A child whose name carries a dot, such as <c>&lt;Grid.RowDefinitions&gt;</c>. XAML treats it
        /// as a property of the surrounding element, not as a child of it.
        /// </summary>
        private static bool IsPropertyElement(XElement element)
        {
            return element.Name.LocalName.Contains('.');
        }

        private PropertyElement ToPropertyElement(XElement element, XamlParseContext context)
        {
            var localName = element.Name.LocalName;
            var separator = localName.IndexOf('.');

            return new PropertyElement
            {
                Name = localName[(separator + 1)..],
                OwnerTypeName = localName[..separator],
                Prefix = element.GetPrefixOfNamespace(element.Name.Namespace) ?? string.Empty,
                XmlNamespace = element.Name.NamespaceName,
                Location = context.LocationOf(element),
                Children = element.Elements().Select(e => Build(e, context)).ToImmutableList()
            };
        }

        /// <summary>
        /// <c>x:Name</c> wins over a plain <c>Name</c>, which is the order WPF resolves them in. Both
        /// stay in <see cref="ElementBase.Properties"/>.
        /// </summary>
        private static string NameOf(ImmutableList<Property> properties)
        {
            var directive = properties.FirstOrDefault(p => p.IsXamlDirective && p.Name.EqualsTo("Name"));
            var plain = properties.FirstOrDefault(p => !p.IsXamlDirective && p.Name.EqualsTo("Name"));

            return (directive ?? plain)?.PropertyValue?.ValueText ?? string.Empty;
        }

        /// <summary>The text written directly inside the element, null when it has none of its own.</summary>
        private static string? ContentOf(XElement element)
        {
            var text = string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value)).Trim();

            return text.IsNullOrEmpty() ? null : text;
        }

        private static DataContext? ToDataContext(Property? property)
        {
            if (property?.PropertyValue is not { } value)
            {
                return null;
            }

            var name = value switch
            {
                XTypeMarkupExtension type => type.Type,
                ResourceReference resource => resource.ResourceKey,
                Binding binding => binding.Path?.ValueText ?? string.Empty,
                MarkupExtension markup => markup["Type"]?.PropertyValue?.ValueText
                                          ?? markup.PositionalArguments.FirstOrDefault()?.Split(':').Last()
                                          ?? string.Empty,
                _ => value.ValueText
            };

            return new DataContext { FullQualifiedName = name, Value = value };
        }

        /// <summary>
        /// Links every child back to the element it is written in, property element children included.
        /// Done after the node exists, because the node has to exist before it can be a parent.
        /// </summary>
        private static void Reparent(ElementBase element)
        {
            foreach (var child in element.Children)
            {
                child.Parent = element;
                Reparent(child);
            }

            foreach (var child in element.PropertyElements.SelectMany(p => p.Children))
            {
                child.Parent = element;
                Reparent(child);
            }
        }

        private static ElementBase Apply(ElementBase element, ElementParts parts)
        {
            return element with
            {
                TypeName = parts.TypeName,
                Prefix = parts.Prefix,
                XmlNamespace = parts.XmlNamespace,
                Location = parts.Location,
                XName = parts.XName,
                XKey = parts.XKey,
                Content = parts.Content,
                DataContext = parts.DataContext,
                Properties = parts.Properties,
                Children = parts.Children
            };
        }

        private static ElementBase CreateEmpty(string typeName)
        {
            return typeName switch
            {
                "Style" => new Style(),
                "Setter" => new Setter(),
                "ResourceDictionary" => new ResourceDictionaryElement(),
                "DataTemplate" or "ControlTemplate" or "ItemsPanelTemplate" or "HierarchicalDataTemplate" => new Template(),
                var name when name.EndWith("Trigger") => new Trigger(),
                _ => new Control()
            };
        }

        private static Root CreateEmptyRoot(string typeName)
        {
            return typeName switch
            {
                "Window" => new Window(),
                "UserControl" => new UserControl(),
                "Page" => new Page(),
                "Application" => new Application(),
                "ResourceDictionary" => new ResourceDictionaryRoot(),
                _ => new ControlRoot()
            };
        }

        /// <summary>Everything the concrete element types share, gathered once instead of seven times.</summary>
        private sealed record ElementParts
        {
            internal required string TypeName { get; init; }

            internal required string Prefix { get; init; }

            internal required string XmlNamespace { get; init; }

            internal required XamlLocation Location { get; init; }

            internal required string XName { get; init; }

            internal required string XKey { get; init; }

            internal required string? Content { get; init; }

            internal required DataContext? DataContext { get; init; }

            internal required ImmutableList<Property> Properties { get; init; }

            internal required ImmutableList<ElementBase> Children { get; init; }
        }
    }
}
