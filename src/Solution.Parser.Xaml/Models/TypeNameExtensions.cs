using System.Linq;

namespace Solution.Parser.Xaml
{
    internal static class TypeNameExtensions
    {
        /// <summary>
        /// The type a value names, whether it is written plainly, with a prefix or wrapped in
        /// <c>{x:Type ...}</c>. All three spellings mean the same type, so a rule comparing against a
        /// type name should not have to know which one the author picked.
        /// </summary>
        internal static string ToTypeName(this PropertyValue? value)
        {
            return value switch
            {
                null => string.Empty,
                XTypeMarkupExtension type => type.Type,
                _ => value.ValueText.Split(':').Last()
            };
        }
    }
}
