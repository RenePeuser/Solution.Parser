using System;
using System.Diagnostics;
using System.IO;
using Argument.Check;
using Extensions.Pack;
using FileSystemInfoExtensions = Solution.Parser.Core.FileSystemInfoExtensions;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Value.FullName}")]
    public abstract class SpecificFileInfoBase : ImmutableSemanticType<FileInfo>
    {
        protected SpecificFileInfoBase(string path,
                                       string expectedFileExtension)
            : base(new FileInfo(path))
        {
            Throw.IfNullOrEmpty(path);

            Throw.IfNullOrEmpty(expectedFileExtension);

            if (!path.EndWith(expectedFileExtension))
            {
                throw new ArgumentException($"The given path '{path}' has not the expected file extension '{expectedFileExtension}'");
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
