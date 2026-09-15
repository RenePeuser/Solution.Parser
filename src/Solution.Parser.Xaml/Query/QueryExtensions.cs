using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The recursive walk over a parsed file. <see cref="ElementBase.Children"/> holds only what an
    /// element declares itself, so anything that needs the whole tree asks for it here.
    /// </summary>
    /// <remarks>
    /// This replaces the old eager <c>Styles</c> and <c>DataTemplates</c> lists. Those sat on every
    /// element, were filled from every descendant, and in the case of <c>Styles</c> were always empty
    /// because no builder ever produced a style.
    /// </remarks>
    public static class QueryExtensions
    {
        /// <summary>Every element in the file, the root included.</summary>
        public static ImmutableList<ElementBase> AllElements(this XamlSyntaxTree syntaxTree)
        {
            return syntaxTree.Root.SelfAndDescendantElements();
        }

        /// <summary>Every element below this one, at any depth, property element children included.</summary>
        public static ImmutableList<ElementBase> DescendantElements(this ElementBase element)
        {
            return element.Descendants().ToImmutableList();
        }

        /// <summary>This element and everything below it.</summary>
        public static ImmutableList<ElementBase> SelfAndDescendantElements(this ElementBase element)
        {
            return element.Descendants().Prepend(element).ToImmutableList();
        }

        /// <summary>The elements this one is declared in, nearest first.</summary>
        public static ImmutableList<ElementBase> Ancestors(this ElementBase element)
        {
            var ancestors = ImmutableList.CreateBuilder<ElementBase>();

            for (var parent = element.Parent; parent is not null; parent = parent.Parent)
            {
                ancestors.Add(parent);
            }

            return ancestors.ToImmutable();
        }

        public static ImmutableList<ElementBase> AncestorsAndSelf(this ElementBase element)
        {
            return element.Ancestors().Insert(0, element);
        }

        public static ImmutableList<Style> AllStyles(this ElementBase element)
        {
            return element.SelfAndDescendantElements().OfType<Style>().ToImmutableList();
        }

        public static ImmutableList<Style> AllStyles(this XamlSyntaxTree syntaxTree)
        {
            return syntaxTree.Root.AllStyles();
        }

        /// <summary>Every <c>DataTemplate</c>, <c>ControlTemplate</c>, <c>ItemsPanelTemplate</c> and <c>HierarchicalDataTemplate</c>.</summary>
        public static ImmutableList<Template> AllTemplates(this ElementBase element)
        {
            return element.SelfAndDescendantElements().OfType<Template>().ToImmutableList();
        }

        public static ImmutableList<Template> AllTemplates(this XamlSyntaxTree syntaxTree)
        {
            return syntaxTree.Root.AllTemplates();
        }

        /// <summary>Only the data templates, leaving control and panel templates out.</summary>
        public static ImmutableList<Template> AllDataTemplates(this XamlSyntaxTree syntaxTree)
        {
            return syntaxTree.Root
                             .AllTemplates()
                             .Where(t => t.TypeName.EndWith("DataTemplate"))
                             .ToImmutableList();
        }

        public static ImmutableList<Setter> AllSetters(this ElementBase element)
        {
            return element.SelfAndDescendantElements().OfType<Setter>().ToImmutableList();
        }

        public static ImmutableList<Trigger> AllTriggers(this ElementBase element)
        {
            return element.SelfAndDescendantElements().OfType<Trigger>().ToImmutableList();
        }

        /// <summary>Everything declared in any <c>&lt;X.Resources&gt;</c> in the file.</summary>
        public static ImmutableList<ElementBase> AllResources(this XamlSyntaxTree syntaxTree)
        {
            return syntaxTree.AllElements().SelectMany(e => e.Resources).ToImmutableList();
        }

        /// <summary>Every markup extension used anywhere in the file, nested ones included.</summary>
        public static ImmutableList<MarkupExtension> AllMarkupExtensions(this XamlSyntaxTree syntaxTree)
        {
            return syntaxTree.AllElements()
                             .SelectMany(e => e.Properties)
                             .SelectMany(p => p.SelfAndNestedValues())
                             .OfType<MarkupExtension>()
                             .ToImmutableList();
        }

        public static ImmutableList<Binding> AllBindings(this XamlSyntaxTree syntaxTree)
        {
            return syntaxTree.AllMarkupExtensions().OfType<Binding>().ToImmutableList();
        }

        /// <summary>The element with this <c>x:Name</c>, null when the file declares none.</summary>
        public static ElementBase? FindByName(this XamlSyntaxTree syntaxTree, string name)
        {
            return syntaxTree.AllElements().FirstOrDefault(e => e.XName.EqualsTo(name));
        }

        /// <summary>The element with this <c>x:Key</c>, null when the file declares none.</summary>
        public static ElementBase? FindByKey(this XamlSyntaxTree syntaxTree, string key)
        {
            return syntaxTree.AllElements().FirstOrDefault(e => e.XKey.EqualsTo(key));
        }

        /// <summary>Every element written with this type name, for example every <c>Button</c>.</summary>
        public static ImmutableList<ElementBase> OfTypeName(this XamlSyntaxTree syntaxTree, string typeName)
        {
            return syntaxTree.AllElements().Where(e => e.TypeName.EqualsTo(typeName)).ToImmutableList();
        }

        private static IEnumerable<ElementBase> Descendants(this ElementBase element)
        {
            var children = element.Children.Concat(element.PropertyElements.SelectMany(p => p.Children));

            foreach (var child in children)
            {
                yield return child;

                foreach (var descendant in child.Descendants())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// A property value plus everything nested inside it, so a converter named inside a binding is
        /// found as well as the binding itself.
        /// </summary>
        private static IEnumerable<PropertyValue> SelfAndNestedValues(this Property property)
        {
            return property.PropertyValue is null
                ? Enumerable.Empty<PropertyValue>()
                : property.PropertyValue.SelfAndNestedValues();
        }

        private static IEnumerable<PropertyValue> SelfAndNestedValues(this PropertyValue value)
        {
            yield return value;

            if (value is not MarkupExtension markup)
            {
                yield break;
            }

            foreach (var nested in markup.Properties.SelectMany(p => p.SelfAndNestedValues()))
            {
                yield return nested;
            }
        }
    }
}
