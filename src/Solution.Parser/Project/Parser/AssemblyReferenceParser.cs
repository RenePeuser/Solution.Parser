using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class AssemblyReferenceParser
    {
        internal static AssemblyReference Parse(XElement element)
        {
            var include = element.AttributeBy(ParserHelper.Include)?.Value ?? string.Empty;
            var hintPath = element.ElementBy(ParserHelper.HintPath)?.ValueOrDefault(string.Empty) ?? string.Empty;
            var copyLocal = element.ElementBy(ParserHelper.Private)?.ToBool(hintPath.IsNotEmpty()) ?? false;
            var name = element.AttributeBy(ParserHelper.Include)?.Value ?? string.Empty;
            name = name.Split(',').FirstOrDefault();

            var includeContainsVersion = include.Contains(ParserHelper.Version);
            var specificVersion = element.ElementBy(ParserHelper.SpecificVersion)?.ToBool(includeContainsVersion) ?? false;

            return new AssemblyReference(include, copyLocal, hintPath, specificVersion, name ?? string.Empty);
        }
    }
}
