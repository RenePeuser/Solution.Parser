namespace Solution.Parser.Project
{
    internal static class ParserHelper
    {
        private const string VERSION = "Version=";

        internal static string Version => VERSION;

        internal static string HintPath { get; } = "HintPath";

        internal static string SpecificVersion { get; } = "SpecificVersion";

        internal static string Private { get; } = "Private";

        internal static string Name { get; } = "Name";

        internal static string AssemblyName { get; } = "AssemblyName";

        internal static string ProjectGuid { get; } = "ProjectGuid";

        internal static string Compile { get; } = "Compile";

        internal static string Page { get; } = "Page";

        internal static string ProjectTypeGuids { get; } = "ProjectTypeGuids";

        internal static string Import { get; } = "Import";

        internal static string ProjectReference { get; } = "ProjectReference";

        internal static string Reference { get; } = "Reference";

        internal static string Content { get; } = "Content";

        internal static string TargetFrameworkVersion { get; } = "TargetFrameworkVersion";

        internal static string TargetFrameworkNewFormat { get; } = "TargetFramework";

        internal static string TargetFrameworksNewFormat { get; } = "TargetFrameworks";

        internal static string BuildRoot { get; } = "BUILD_ROOT";

        internal static string DocumentationFile { get; } = "DocumentationFile";

        internal static string CopyToOutputDirectory { get; } = "CopyToOutputDirectory";

        internal static string Include { get; } = "Include";

        internal static string Update { get; } = "Update";

        internal static string Project { get; } = "Project";
    }
}
