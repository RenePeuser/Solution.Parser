using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Enumeration;
using Argument.Check;

namespace Solution.Parser.Project
{
    internal static class SourceFileFilter
    {
        // What EnumerateFiles("*.*", AllDirectories) used, so the files come in the same order as before.
        private static readonly EnumerationOptions Options = new()
                                                             {
                                                                 RecurseSubdirectories = true,
                                                                 AttributesToSkip = 0,
                                                                 IgnoreInaccessible = false
                                                             };

        /// <summary>
        /// Everything under the project directory except build output. A bin or obj folder is not even
        /// entered, which matters: it often holds more files than the project itself. Only folders below
        /// the project directory count, so a repository that itself sits under a path containing
        /// <c>bin</c> or <c>obj</c> does not lose all of its files.
        /// </summary>
        internal static ImmutableList<FileInfo> SourceFilesBelow(DirectoryInfo projectDirectory)
        {
            Throw.IfNull(projectDirectory);

            var files = new FileSystemEnumerable<FileInfo>(projectDirectory.FullName,
                                                           (ref entry) => (FileInfo)entry.ToFileSystemInfo(),
                                                           Options)
                        {
                            ShouldIncludePredicate = (ref entry) => !entry.IsDirectory,
                            ShouldRecursePredicate = (ref entry) => !IsBuildOutput(entry.FileName)
                        };

            return files.ToImmutableList();
        }

        private static bool IsBuildOutput(ReadOnlySpan<char> directoryName)
        {
            return directoryName.SequenceEqual("bin") || directoryName.SequenceEqual("obj");
        }
    }
}
