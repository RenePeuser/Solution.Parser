using Solution.Parser.Core;

namespace Solution.Parser.Project
{
    public class StyleCopSettingsFileInfo : SpecificFileInfo
    {
        internal StyleCopSettingsFileInfo(string path)
            : base(path, ".StyleCop")
        {
        }
    }
}
