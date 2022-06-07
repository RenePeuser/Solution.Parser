using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.Nuspec
{
    [DebuggerDisplay("{NuspecFileInfo.FileNameWithoutExtenion}")]
    public class NuspecFile
    {
        internal NuspecFile(NuspecFileInfo nuspecFileInfo, XDocument document,
            IImmutableList<FrameworkAssembly> frameworkAssemblies, IImmutableList<Dependency> dependencies)
        {
            Throw.IfNull(nuspecFileInfo);
            Throw.IfNull(document);
            Throw.IfNull(frameworkAssemblies);
            Throw.IfNull(dependencies);

            NuspecFileInfo = nuspecFileInfo;
            Document = document;
            FrameworkAssemblies = frameworkAssemblies;
            Dependencies = dependencies;
        }

        public NuspecFileInfo NuspecFileInfo { get; }

        public XDocument Document { get; }

        public IImmutableList<FrameworkAssembly> FrameworkAssemblies { get; }

        public IImmutableList<Dependency> Dependencies { get; }
    }
}
