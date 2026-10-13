using System.IO;
using System.Linq;
using System.Xml.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class ProjectFileParser
    {
        internal static ProjectFile Parse(FileInfo projectFileInfo)
        {
            Throw.IfNull(projectFileInfo);

            var document = XDocument.Load(projectFileInfo.FullName);
            var documentRoot = document.Root;

            Throw.IfNull(documentRoot);

            // new project format, temp check
            if (documentRoot!.Attributes().Any(a => a.Name.LocalName.ToUpperInvariant().EqualsTo("SDK")))
            {
                return ProjectParserNewFormat.Parse(projectFileInfo, document);
            }

            return ProjectParserOldFormat.Parse(projectFileInfo, document);
        }
    }
}
