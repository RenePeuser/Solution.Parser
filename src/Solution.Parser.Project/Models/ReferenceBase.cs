using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("{Include}")]
    public abstract class ReferenceBase(string include,
                                        bool copyLocal,
                                        string name)
    {
        public string Include { get; } = include;

        public bool CopyLocal { get; } = copyLocal;

        public string Name { get; } = name;
    }
}
