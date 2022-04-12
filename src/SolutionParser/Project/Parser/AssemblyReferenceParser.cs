using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace SolutionParser.Project
{
    internal static class AssemblyReferenceParser
    {
        internal static AssemblyReference Parse(XElement element)
        {
            var include = element.AttributeBy(ParserHelper.Include).Value;

            var hintPath = element.ElementBy(ParserHelper.HintPath).ValueOrDefault(string.Empty);
            var copyLocal = element.ElementBy(ParserHelper.Private).ToBool(hintPath.IsNotEmpty());
            var name = element.AttributeBy(ParserHelper.Include).Value;
            name = name.Split(',').FirstOrDefault();

            var includeContainsVersion = include.Contains(ParserHelper.Version);
            var specificVersion = element.ElementBy(ParserHelper.SpecificVersion).ToBool(includeContainsVersion);

            return new AssemblyReference(include, copyLocal, hintPath, specificVersion, name);
        }
    }
}
