using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Argument.Check;

namespace Solution.Parser.Core
{
    /// <summary>
    /// A file of a known kind, such as a <c>.cs</c>, <c>.xaml</c>, <c>.csproj</c> or <c>.sln</c> file. The
    /// kind is what the typed <c>Parse()</c> of each package hangs on; the rest is the file itself, without
    /// a <c>.Value</c> in between.
    /// </summary>
    /// <remarks>
    /// Two infos of the same file are equal, so they work as dictionary keys and in <c>Distinct()</c>.
    /// </remarks>
    [DebuggerDisplay("{FullName}")]
    public abstract class SpecificFileInfo : IEquatable<SpecificFileInfo>
    {
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        protected SpecificFileInfo(string path, params string[] expectedFileExtensions)
        {
            Throw.IfNullOrEmpty(path);
            Throw.IfNullOrEmpty(expectedFileExtensions);

            if (!expectedFileExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"The given path '{path}' has not one of the expected file extensions '{string.Join("', '", expectedFileExtensions)}'");
            }

            if (!System.IO.File.Exists(path))
            {
                throw new ArgumentException($"The given path '{path}' does not exist.");
            }

            File = new FileInfo(path);
        }

        public FileInfo File { get; }

        /// <summary>The full path, <c>D:\repo\src\Person.cs</c>.</summary>
        public string FullName => File.FullName;

        /// <summary>The file name with extension, <c>Person.cs</c>.</summary>
        public string Name => File.Name;

        /// <summary>The file name without extension, <c>Person</c>.</summary>
        public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(File.Name);

        public DirectoryInfo Directory => File.Directory!;

        public bool Equals(SpecificFileInfo? other)
        {
            return other is not null && other.GetType() == GetType() && PathComparer.Equals(FullName, other.FullName);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as SpecificFileInfo);
        }

        public override int GetHashCode()
        {
            return PathComparer.GetHashCode(FullName);
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
