using System;
using System.Diagnostics;
using System.IO;
using Extensions.Pack;
using SolutionParser.Common;

namespace SolutionParser.Project
{
    [DebuggerDisplay("{Value.FullName}")]
    public abstract class SpecificFileInfoBase : ImmutableSemanticType<FileInfo>
    {
        protected SpecificFileInfoBase(string path, string expectedFileExtension)
            : base(new FileInfo(path))
        {
            if (path.IsNullOrEmpty())
            {
                throw new ArgumentException("The path of a file info must not be null or empty", nameof(path));
            }

            if (expectedFileExtension.IsNullOrEmpty())
            {
                throw new ArgumentException("The expected file extension must not be null",
                    nameof(expectedFileExtension));
            }

            if (!path.EndWith(expectedFileExtension))
            {
                throw new ArgumentException(
                    $"The given path '{path}' has not the expected file extension '{expectedFileExtension}'");
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The given path '{path}'does not exists: ");
            }

            FileNameWithoutExtenion = Value.FileNameWithoutExtension();
        }

        public string FileNameWithoutExtenion { get; }
    }
}
