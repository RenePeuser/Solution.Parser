using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Project}")]
    public class Import
    {
        internal Import(string project)
        {
            Project = project;
        }

        public string Project { get; }
    }
}
