using System.Diagnostics;

namespace SolutionParser.Project
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ProjectReference : ReferenceBase
    {
        internal ProjectReference(string include, bool copyLocal, string name)
            : base(include, copyLocal, name)
        {
        }
    }
}
