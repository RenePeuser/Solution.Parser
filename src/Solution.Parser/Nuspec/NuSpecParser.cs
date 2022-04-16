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
                    element.Attribute("targetFramework").Value)).ToList();
            var dependencies = document.ElementsBy("dependency")
                .Select(element => new Dependency(element.Attribute("id").Value,
                    element.Attribute("version").Value,
                    element.Attribute("exclude").IsNull()
                        ? Enumerable.Empty<string>()
                        : element.Attribute("exclude").Value.Split(',').ToList()))
                .ToList();

            return new NuspecFile(nuspecFileInfo, document, frameworkAssemblies, dependencies);
        }
    }
}
