using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
using FileSystemInfoExtensions = Solution.Parser.Common.FileSystemInfoExtensions;

namespace Solution.Parser.Solution
{
    [DebuggerDisplay("{Value.FullName}")]
    public abstract class SpecificFileInfoBase : ImmutableSemanticType<FileInfo>
    {
        protected SpecificFileInfoBase(string path, params string[] expectedFileExtensions)
            : base(new FileInfo(path))
        {
            Throw.IfNullOrEmpty(path);

            Throw.IfNullOrEmpty(expectedFileExtensions);

            if (!expectedFileExtensions.Any(path.EndWith))
            {
                throw new ArgumentException(
                    $"The given path '{path}' has not one of the expected file extensions '{string.Join("', '", expectedFileExtensions)}'");
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The given path '{path}'does not exists: ");
            }

            FileNameWithoutExtenion = FileSystemInfoExtensions.FileNameWithoutExtension(Value);
        }

        public string FileNameWithoutExtenion { get; }
    }
}
