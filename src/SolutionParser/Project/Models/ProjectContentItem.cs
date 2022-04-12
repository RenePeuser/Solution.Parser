using System.Diagnostics;
using System.IO;

namespace SolutionParser.Project
{
    [DebuggerDisplay("{" + nameof(Include) + "}")]
    [DebuggerDisplay("{" + nameof(CopyToOutputDirectory) + "}")]
    public class ProjectContentItem
    {
        internal ProjectContentItem(string include, CopyToOutputDirectory copyToOutputDirectory, FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            CopyToOutputDirectory = copyToOutputDirectory;
            Include = include;
        }

        public string Include { get; }

        public CopyToOutputDirectory CopyToOutputDirectory { get; }

        public FileInfo FileInfo { get; }
    }
}
