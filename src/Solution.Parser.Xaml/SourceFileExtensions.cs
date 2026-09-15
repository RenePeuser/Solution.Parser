using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Xaml
{
    /// <summary>
    /// The XAML view on a project's files: <c>project.SourceFiles.XamlFiles()</c>.
    /// </summary>
    /// <remarks>
    /// This lives here rather than on <c>ProjectFile</c> so that the project package stays language
    /// agnostic. Extending the file list instead of <c>ProjectFile</c> keeps this package free of a
    /// dependency on it, and works on any list of files.
    /// </remarks>
    public static class SourceFileExtensions
    {
        public static ImmutableList<XamlFileInfo> XamlFiles(this IEnumerable<FileInfo> sourceFiles)
        {
            Throw.IfNull(sourceFiles);

            return sourceFiles.Where(IsXamlFile)
                              .Select(file => new XamlFileInfo(file.FullName))
                              .ToImmutableList();
        }

        /// <summary>Generated files carry a <c>.g.</c> in their name and are not source a rule should judge.</summary>
        private static bool IsXamlFile(FileInfo file)
        {
            return file.Name.EndWith(".xaml") && !file.Name.Contains(".g.");
        }
    }
}
