using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.Nuspec
{
    [DebuggerDisplay("{FileInfo.Name}")]
    public class NuspecFile
    {
        /// <summary>Parses the nuspec at <paramref name="path"/>.</summary>
        public static NuspecFile Parse(string path)
        {
            Throw.IfNullOrWhiteSpace(path);

            return Parse(new FileInfo(path));
        }

        /// <summary>Parses the given nuspec.</summary>
        public static NuspecFile Parse(FileInfo fileInfo)
        {
            return NuSpecParser.Parse(fileInfo);
        }

        internal NuspecFile(FileInfo fileInfo, XDocument document,
            ImmutableList<FrameworkAssembly> frameworkAssemblies, ImmutableList<Dependency> dependencies)
        {
            Throw.IfNull(fileInfo);
            Throw.IfNull(document);
            Throw.IfNull(frameworkAssemblies);
            Throw.IfNull(dependencies);

            FileInfo = fileInfo;
            Document = document;
            FrameworkAssemblies = frameworkAssemblies;
            Dependencies = dependencies;
        }

        public FileInfo FileInfo { get; }

        public XDocument Document { get; }

        public ImmutableList<FrameworkAssembly> FrameworkAssemblies { get; }

        public ImmutableList<Dependency> Dependencies { get; }
    }
}
