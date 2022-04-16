using System.Diagnostics;

namespace Solution.Parser.Project
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
