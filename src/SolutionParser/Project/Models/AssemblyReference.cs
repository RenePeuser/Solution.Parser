using System.Diagnostics;

namespace SolutionParser.Project
{
    [DebuggerDisplay("Reference = {" + nameof(HintPath) + "}")]
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
