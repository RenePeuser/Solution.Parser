using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    internal sealed class StaticResourceParser : IMarkupExtensionParser
    {
        public bool IsThisTheCorrectParserFor(MarkupToken token)
        {
            return token.Name.EqualsTo("StaticResource") || token.Name.EqualsTo("ThemeResource");
        }

        public PropertyValue Parse(MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            return new StaticResource(token.RawValue)
            {
                Name = token.Name,
                Prefix = token.Prefix,
                PositionalArguments = token.PositionalArguments,
                Properties = token.ToProperties(location, context),
                ResourceKey = token.ToResourceKey(location, context)
            };
        }
    }

    internal static class ResourceReferenceExtensions
    {
        /// <summary>
        /// The key being looked up. A key written as <c>{x:Type Button}</c> resolves to the type name,
        /// so both spellings of a default style key compare equal.
        /// </summary>
        internal static string ToResourceKey(this MarkupToken token, XamlLocation location, XamlParseContext context)
        {
            var key = token.NamedOrFirstPositional("ResourceKey");

            if (key.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            return MarkupExtensionTokenizer.IsMarkupExtension(key)
                ? key.ToTypeName(location, context)
                : key.Split(':').Last();
        }
    }
}
