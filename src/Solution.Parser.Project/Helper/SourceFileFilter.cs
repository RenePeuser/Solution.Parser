using System.IO;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class SourceFileFilter
    {
        /// <summary>
        /// Everything under the project directory except build output. The relative path is checked
        /// rather than the full one, so a repository that itself sits under a path containing
        /// <c>bin</c> or <c>obj</c> does not lose all of its files.
        /// </summary>
        internal static bool IsSourceFile(FileInfo file, DirectoryInfo projectDirectory)
        {
            Throw.IfNull(file);
            Throw.IfNull(projectDirectory);

            var relativePath = file.FullName.Replace(projectDirectory.FullName, string.Empty);

            return relativePath.DoesNotContain(@"\bin\") && relativePath.DoesNotContain(@"\obj\");
        }
    }
}
