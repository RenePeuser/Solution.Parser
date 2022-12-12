using System.Linq;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.Project
{
    public static class ProjectFileParser
    {
        public static ProjectFile Parse(ProjectFileInfo projectFileInfo)
        {
            var document = XDocument.Load(projectFileInfo.Value.FullName);
            var documentRoot = document.Root;

            Throw.IfNull(documentRoot);

            // new project format, temp check
            if (documentRoot!.Attributes().Any(a => a.Name.LocalName.ToUpperInvariant() == "SDK"))
            {
                return ProjectParserNewFormat.Parse(projectFileInfo, document);
            }

            return ProjectParserOldFormat.Parse(projectFileInfo, document);
        }
    }
}
