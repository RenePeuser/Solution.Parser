using System.Diagnostics;

namespace SolutionParser.Project
{
    [DebuggerDisplay("{" + nameof(Project) + "}")]
    public class Import
    {
        internal Import(string project)
        {
            Project = project;
        }

        public string Project { get; }
    }
}
