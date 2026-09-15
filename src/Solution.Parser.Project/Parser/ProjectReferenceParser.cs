using System.IO;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class ProjectReferenceParser
    {
        internal static ProjectReference Parse(XElement element)
        {
            // AttributeBy(...).ToString() renders the whole attribute, as in Include="..\A\A.csproj".
            // AssemblyReferenceParser reads .Value here, and so does this one now.
            var include = element.AttributeBy(ParserHelper.Include)?.Value ?? string.Empty;
            var copyLocal = element.ElementBy(ParserHelper.Private)?.ToBool(true) ?? true;

            // An SDK style project reference carries no <Name> element. MSBuild takes the file name of
            // the referenced project in that case, and without it every such reference was nameless.
            var name = element.ElementBy(ParserHelper.Name)?.ValueOrDefault(string.Empty) ?? string.Empty;
            var resolvedName = name.IsNullOrWhiteSpace() ? NameFromInclude(include) : name;

            return new ProjectReference(include, copyLocal, resolvedName);
        }

        private static string NameFromInclude(string include)
        {
            return include.IsNullOrWhiteSpace()
                ? string.Empty
                : Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar));
        }
    }
}
