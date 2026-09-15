using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The C# view on a project's files: <c>project.SourceFiles.CSharpFiles()</c>.
    /// </summary>
    /// <remarks>
    /// This lives here rather than on <c>ProjectFile</c> so that the project package stays language
    /// agnostic. Extending the file list instead of <c>ProjectFile</c> keeps this package free of a
    /// dependency on it, and works on any list of files.
    /// </remarks>
    public static class SourceFileExtensions
    {
        public static ImmutableList<CSharpFileInfo> CSharpFiles(this IEnumerable<FileInfo> sourceFiles)
        {
            Throw.IfNull(sourceFiles);

            return sourceFiles.Where(IsCSharpFile)
                              .Select(file => new CSharpFileInfo(file.FullName))
                              .ToImmutableList();
        }

        /// <summary>Generated files carry a <c>.g.</c> in their name and are not source a rule should judge.</summary>
        private static bool IsCSharpFile(FileInfo file)
        {
            return file.Name.EndWith(".cs") && !file.Name.Contains(".g.");
        }
    }
}
