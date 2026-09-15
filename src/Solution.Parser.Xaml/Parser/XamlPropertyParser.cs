using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// Turns an XML attribute into a <see cref="Property"/>.
    /// </summary>
    /// <remarks>
    /// The prefix is kept rather than dropped. Without it <c>x:Name</c> and a plain <c>Name</c> are the
    /// same property, which is what the old parser reported, and a rule asking for the directive got
    /// whichever of the two came first.
    /// </remarks>
    internal sealed class XamlPropertyParser
    {
        internal Property ParseFrom(XAttribute attribute, XamlParseContext context)
        {
            var location = context.LocationOf(attribute);
            var localName = attribute.Name.LocalName;
            var separator = localName.IndexOf('.');
            var value = context.ParseValue(attribute.Value, location);

            if (separator <= 0)
            {
                return new Property
                {
                    Name = localName,
                    Prefix = PrefixOf(attribute),
                    XmlNamespace = attribute.Name.NamespaceName,
                    Location = location,
                    PropertyValue = value
                };
            }

            return new AttachedProperty
            {
                Name = localName[(separator + 1)..],
                OwnerTypeName = localName[..separator],
                Prefix = PrefixOf(attribute),
                XmlNamespace = attribute.Name.NamespaceName,
                Location = location,
                PropertyValue = value
            };
        }

        /// <summary>
        /// Builds the model for an <c>xmlns</c> declaration. These are namespace bookkeeping rather
        /// than properties of the element, so they are collected on the root instead of mixed into
        /// <see cref="ElementBase.Properties"/>.
        /// </summary>
        internal XamlUsing ToXamlUsing(XAttribute attribute)
        {
            const string clrNamespace = "clr-namespace:";

            var alias = attribute.Name.NamespaceName.EqualsTo(XamlNamespaces.Xmlns)
                ? attribute.Name.LocalName
                : string.Empty;

            var value = attribute.Value;
            if (!value.StartWith(clrNamespace))
            {
                return new XamlUsing(value) { Alias = alias, Namespace = value };
            }

            var parts = value[clrNamespace.Length..].Split(';');
            var assembly = parts.Skip(1)
                                .Select(p => p.Split('='))
                                .Where(p => p.Length > 1 && p[0].Trim().EqualsTo("assembly"))
                                .Select(p => p[1].Trim())
                                .FirstOrDefault() ?? string.Empty;

            return new XamlUsing(value)
            {
                Alias = alias,
                Namespace = parts[0],
                Assembly = assembly,
                IsClrNamespace = true
            };
        }

        private static string PrefixOf(XAttribute attribute)
        {
            if (attribute.Name.NamespaceName.IsNullOrEmpty())
            {
                return string.Empty;
            }

            return attribute.Parent?.GetPrefixOfNamespace(attribute.Name.Namespace) ?? string.Empty;
        }
    }
}
