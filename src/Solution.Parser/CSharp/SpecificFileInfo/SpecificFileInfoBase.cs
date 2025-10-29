using System;
using System.Diagnostics;
using System.IO;
using Argument.Check;
using Extensions.Pack;
using FileSystemInfoExtensions = Solution.Parser.Common.FileSystemInfoExtensions;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Value.FullName}")]
    public abstract record SpecificFileInfoBase : ImmutableSemanticType<FileInfo>
    {
        protected SpecificFileInfoBase(string path,
                                       string expectedFileExtension)
            : base(new FileInfo(path))
        {
            Throw.IfNullOrEmpty(path);

            Throw.IfNullOrEmpty(expectedFileExtension);

            if (path.EndWith(expectedFileExtension).IsFalse())
            {
                throw new ArgumentException($"The given path '{path}' has not the expected file extension '{expectedFileExtension}'");
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The given path '{path}'does not exists: ");
            }

            FileNameWithoutExtension = FileSystemInfoExtensions.FileNameWithoutExtension(Value);
        }

        public string FileNameWithoutExtension { get; }
    }
}
