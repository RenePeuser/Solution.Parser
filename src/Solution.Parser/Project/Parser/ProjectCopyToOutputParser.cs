using System.Collections.Generic;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class ProjectCopyToOutputParser
    {
        private static readonly Dictionary<string, CopyToOutputDirectory> SProjectCopyToOutputMapping = new()
        {
            { "Always", CopyToOutputDirectory.CopyAlways },
            { "PreserveNewest", CopyToOutputDirectory.CopyIfNewer }
        };

        internal static CopyToOutputDirectory ToCopyToOutputDirectory(this string guid)
        {
            return guid.IsNullOrWhiteSpace()
                ? CopyToOutputDirectory.DoNotCopy
                : DictionaryExtensions.GetValueOrDefault(SProjectCopyToOutputMapping, guid);
        }
    }
}
