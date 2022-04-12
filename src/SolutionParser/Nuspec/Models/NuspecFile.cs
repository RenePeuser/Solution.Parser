using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Argument.Check;

namespace SolutionParser.Nuspec
{
    [DebuggerDisplay("{NuspecFileInfo.FileNameWithoutExtenion}")]
    public class NuspecFile
    {
        internal NuspecFile(NuspecFileInfo nuspecFileInfo, XDocument document,
            List<FrameworkAssembly> frameworkAssemblies, List<Dependency> dependencies)
        {
            Throw.IfNull(() => nuspecFileInfo);
            Throw.IfNull(() => document);
            Throw.IfNull(() => frameworkAssemblies);
            Throw.IfNull(() => dependencies);

            NuspecFileInfo = nuspecFileInfo;
            Document = document;
            FrameworkAssemblies = frameworkAssemblies;
            Dependencies = dependencies;
        }

        public NuspecFileInfo NuspecFileInfo { get; }

        public XDocument Document { get; }

        public List<FrameworkAssembly> FrameworkAssemblies { get; }

        public List<Dependency> Dependencies { get; }
    }
}
