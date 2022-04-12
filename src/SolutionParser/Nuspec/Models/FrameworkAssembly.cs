using System.Diagnostics;
using Argument.Check;

namespace SolutionParser.Nuspec
{
    [DebuggerDisplay("{" + nameof(AssemblyName) + "}")]
    public class FrameworkAssembly
    {
        internal FrameworkAssembly(string assemblyName, string targetFramework)
        {
            Throw.IfNullOrWhiteSpace(() => assemblyName);
            Throw.IfNull(() => targetFramework);

            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
        }

        public string AssemblyName { get; }
        public string TargetFramework { get; }
    }
}
