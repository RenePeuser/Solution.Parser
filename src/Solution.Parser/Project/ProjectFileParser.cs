using System.Linq;
using System.Xml.Linq;

namespace Solution.Parser.Project
{
    public static class ProjectFileParser
    {
        public static ProjectFile Parse(ProjectFileInfo projectFileInfo)
        {
            var document = XDocument.Load(projectFileInfo.Value.FullName);

            // new project format, temp check
            if (document.Root.Attributes().Any(a => a.Name.LocalName.ToUpperInvariant() == "SDK"))
            {
                return ProjectParserNewFormat.Parse(projectFileInfo, document);
            }

            return ProjectParserOldFormat.Parse(projectFileInfo, document);
        }
    }
}
