using System;
using System.IO;

namespace SolutionParser.Nuspec
{
    public static class NugetFolderFinder
    {
        public static DirectoryInfo GetUserNuGetFolder()
        {
            var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nugetDirectory = new DirectoryInfo(Path.Combine(userPath, ".nuget"));
            return nugetDirectory;
        }
    }
}
