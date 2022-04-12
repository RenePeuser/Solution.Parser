using System.Diagnostics;

namespace SolutionParser.Project
{
    [DebuggerDisplay("{" + nameof(Include) + "}")]
    public abstract class ReferenceBase
    {
        protected ReferenceBase(string include, bool copyLocal, string name)
        {
            Include = include;
            CopyLocal = copyLocal;
            Name = name;
        }

        public string Include { get; }

        public bool CopyLocal { get; }

        public string Name { get; }
    }
}
