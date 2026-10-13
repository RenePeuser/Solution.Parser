using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Nuspec
{
    internal static class NuSpecParser
    {
        internal static NuspecFile Parse(FileInfo nuspecFileInfo)
        {
            Throw.IfNull(nuspecFileInfo);

            var document = XDocument.Load(nuspecFileInfo.FullName);


            var frameworkAssemblies = document.ElementsBy("frameworkAssembly").Select(element =>
            {
                return new FrameworkAssembly(element.Attribute("assemblyName")?.Value ?? string.Empty, element.Attribute("targetFramework")?.Value ?? string.Empty);
            }).ToImmutableList();



            var dependencies = document.ElementsBy("dependency")
                .Select(element => new Dependency(element.Attribute("id")?.Value ?? string.Empty,
                    element.Attribute("version")?.Value ?? string.Empty,
                    element.Attribute("exclude").IsNull()
                        ? ImmutableList<string>.Empty
                        : element.Attribute("exclude")?.Value.Split(',').ToImmutableList() ?? ImmutableList<string>.Empty))
                .ToImmutableList();

            return new NuspecFile(nuspecFileInfo, document, frameworkAssemblies, dependencies);
        }
    }
}
