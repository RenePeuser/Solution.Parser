using System.Collections.Generic;
using System.IO;
using Argument.Check;

namespace SolutionParser.Nuspec
{
    public static class NuGetFolderParser
    {
        public static IEnumerable<NuspecFile> Parse(DirectoryInfo directoryInfo)
        {
            Throw.IfNull(() => directoryInfo);

            foreach (var nuspecFileInfo in directoryInfo.EnumerateFiles("*.nuspec", SearchOption.AllDirectories))
            {
                yield return NuSpecParser.Parse(new NuspecFileInfo(nuspecFileInfo.FullName));
            }
        }
    }
}
