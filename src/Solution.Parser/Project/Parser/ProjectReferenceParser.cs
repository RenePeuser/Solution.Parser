using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class ProjectReferenceParser
    {
        internal static ProjectReference Parse(XElement element)
        {
            var include = element.AttributeBy(ParserHelper.Include)?.ToString() ?? string.Empty;
            var copyLocal = element.ElementBy(ParserHelper.Private)?.ToBool(true) ?? true;
            var name = element.ElementBy(ParserHelper.Name)?.ValueOrDefault(string.Empty) ?? string.Empty;

            return new ProjectReference(include, copyLocal, name);
        }
    }
}
