using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Extensions.Pack;

namespace Solution.Parser.Nuspec
{
    public static class NuSpecParser
    {
        public static NuspecFile Parse(NuspecFileInfo nuspecFileInfo)
        {
            var document = XDocument.Load(nuspecFileInfo.Value.FullName);


            var frameworkAssemblies = document.ElementsBy("frameworkAssembly").Select(element =>
                new FrameworkAssembly(element.Attribute("assemblyName").Value,
                    element.Attribute("targetFramework").Value)).ToImmutableList();



            var dependencies = document.ElementsBy("dependency")
                .Select(element => new Dependency(element.Attribute("id").Value,
                    element.Attribute("version").Value,
                    element.Attribute("exclude").IsNull()
                        ? ImmutableList<string>.Empty
                        : element.Attribute("exclude").Value.Split(',').ToImmutableList()))
                .ToImmutableList();

            return new NuspecFile(nuspecFileInfo, document, frameworkAssemblies, dependencies);
        }
    }
}
