using System.IO;
using Argument.Check;
using FileSystem.Abstraction;

namespace Solution.Parser.Common
{
    internal static class FileSystemInfoExtensions
    {
        internal static bool NotExists(this FileSystemInfo fileSystemInfo)
        {
            Throw.IfNull(() => fileSystemInfo);

            return !fileSystemInfo.Exists;
        }

        internal static string FileNameWithoutExtension(this FileSystemInfo fileSystemInfo)
        {
            Throw.IfNull(() => fileSystemInfo);

            return fileSystemInfo.Name.Replace(fileSystemInfo.Extension, string.Empty);
        }

        internal static string FileNameWithoutExtension(this IFileSystemInfo fileSystemInfo)
        {
            Throw.IfNull(() => fileSystemInfo);

            return fileSystemInfo.Name.Replace(fileSystemInfo.Extension, string.Empty);
        }
    }
}
