using System.Collections.Generic;
using Extensions.Pack;

namespace SolutionParser.Project
{
    internal static class ProjectCopyToOutputParser
    {
        private static readonly Dictionary<string, CopyToOutputDirectory> sProjectCopyToOutputMapping = new Dictionary<string, CopyToOutputDirectory>()
        {
            { "Always", CopyToOutputDirectory.CopyAlways },
            { "PreserveNewest", CopyToOutputDirectory.CopyIfNewer }
        };

        internal static CopyToOutputDirectory ToCopyToOutputDirectory(this string guid)
        {
            return guid.IsNullOrWhiteSpace()
                ? CopyToOutputDirectory.DoNotCopy
                : DictionaryExtensions.GetValueOrDefault(sProjectCopyToOutputMapping, guid);
        }
    }
}
