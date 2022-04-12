using System.Diagnostics;

namespace SolutionParser.Project
{
    [DebuggerDisplay("{" + nameof(Value) + "}")]
    public class AssemblyFileInfo : SpecificFileInfoBase
    {
        internal AssemblyFileInfo(string path)
            : base(path, ".dll")
        {
        }
    }
}
