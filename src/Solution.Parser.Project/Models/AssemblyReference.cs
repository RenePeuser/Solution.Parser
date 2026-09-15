using System.Diagnostics;

namespace Solution.Parser.Project
{
    [DebuggerDisplay("Reference = {HintPath}")]
    public class AssemblyReference : ReferenceBase
    {
        internal AssemblyReference(string include, bool copyLocal, string hintPath, bool specificVersion, string name)
            : base(include, copyLocal, name)
        {
            HintPath = hintPath;
            SpecificVersion = specificVersion;
        }

        public string HintPath { get; }

        public bool SpecificVersion { get; }
    }
}
