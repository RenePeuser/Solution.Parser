using System.Xml.Linq;
using Extensions.Pack;

namespace SolutionParser.Project
{
    internal static class ProjectReferenceParser
    {
        internal static ProjectReference Parse(XElement element)
        {
            var include = element.AttributeBy(ParserHelper.Include).ToString();
            var copyLocal = element.ElementBy(ParserHelper.Private).ToBool(true);
            var name = element.ElementBy(ParserHelper.Name).ValueOrDefault(string.Empty);

            return new ProjectReference(include, copyLocal, name);
        }
    }
}
