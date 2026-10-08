using Solution.Parser.Core;

namespace Solution.Parser.Nuspec
{
    public class NuspecFileInfo(string path) : SpecificFileInfo(path, ".nuspec");
}
