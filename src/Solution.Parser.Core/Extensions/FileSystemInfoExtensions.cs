using System.IO;
using Argument.Check;
using FileSystem.Abstraction;

namespace Solution.Parser.Core
{
    /// <summary>
    /// Shared file helpers. Public because every parser package uses them across the assembly
    /// boundary; while everything lived in one assembly they could be internal.
    /// </summary>
    public static class FileSystemInfoExtensions
    {
        public static bool NotExists(this FileSystemInfo fileSystemInfo)
        {
            Throw.IfNull(fileSystemInfo);

            return !fileSystemInfo.Exists;
        }

        public static string FileNameWithoutExtension(this FileSystemInfo fileSystemInfo)
        {
            Throw.IfNull(fileSystemInfo);

            return fileSystemInfo.Name.Replace(fileSystemInfo.Extension, string.Empty);
        }

        public static string FileNameWithoutExtension(this IFileSystemInfo fileSystemInfo)
        {
            Throw.IfNull(fileSystemInfo);

            return fileSystemInfo.Name.Replace(fileSystemInfo.Extension, string.Empty);
        }
    }
}
