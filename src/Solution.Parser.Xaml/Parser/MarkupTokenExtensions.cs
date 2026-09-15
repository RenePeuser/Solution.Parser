using System.Collections.Immutable;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    internal static class MarkupTokenExtensions
    {
        /// <summary>
        /// The named arguments as properties, each value parsed like any other value. Keeping them
        /// means a rule can read an argument the typed model has no field for.
        /// </summary>
        internal static ImmutableList<Property> ToProperties(this MarkupToken token,
                                                             XamlLocation location,
                                                             XamlParseContext context)
        {
            return token.Arguments
                        .Where(a => a.Name is not null)
                        .Select(a => new Property
                        {
                            Name = a.Name!.Split(':').Last(),
                            Prefix = a.Name!.Contains(':') ? a.Name!.Split(':').First() : string.Empty,
                            Location = location,
                            PropertyValue = context.ParseValue(a.Value, location)
                        })
                        .ToImmutableList();
        }

        /// <summary>
        /// The value of a named argument, falling back to the first positional one. Most extensions
        /// accept both spellings, as in <c>{Binding Foo}</c> and <c>{Binding Path=Foo}</c>.
        /// </summary>
        internal static string NamedOrFirstPositional(this MarkupToken token, string name)
        {
            return token[name] ?? token.FirstPositionalArgument;
        }

        /// <summary>
        /// Resolves an argument that names a type, whether written plainly, with a prefix or wrapped in
        /// <c>{x:Type ...}</c>.
        /// </summary>
        internal static string ToTypeName(this string value, XamlLocation location, XamlParseContext context)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            if (!MarkupExtensionTokenizer.IsMarkupExtension(value))
            {
                return value.Split(':').Last();
            }

            return context.ParseValue(value, location) switch
            {
                XTypeMarkupExtension type => type.Type,
                MarkupExtension markup => markup.PositionalArguments.FirstOrDefault()?.Split(':').Last() ?? string.Empty,
                var other => other.ValueText
            };
        }
    }
}
