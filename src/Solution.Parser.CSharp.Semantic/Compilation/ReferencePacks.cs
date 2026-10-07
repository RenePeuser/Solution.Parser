using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The reference assemblies of the shared frameworks, as in
    /// <c>dotnet/packs/Microsoft.NETCore.App.Ref/10.0.0/ref/net10.0</c>.
    /// </summary>
    internal static class ReferencePacks
    {
        private static readonly ConcurrentDictionary<(string Framework, string Tfm), string?> RefDirectories = new();

        /// <summary>
        /// The reference assemblies of the given shared frameworks for the target framework. Falls back
        /// to the assemblies of the running runtime when no reference pack is found, which is close
        /// enough for binding and better than leaving <c>System</c> unresolved.
        /// </summary>
        internal static ImmutableList<string> For(string? targetFramework, ImmutableList<string> frameworkReferences)
        {
            var tfm = BaseTfm(targetFramework);
            var frameworks = frameworkReferences.Contains("Microsoft.NETCore.App")
                                 ? frameworkReferences
                                 : frameworkReferences.Insert(0, "Microsoft.NETCore.App");

            var fromPacks = RefDirectoriesOf(tfm, frameworks).SelectMany(d => Directory.EnumerateFiles(d, "*.dll")).ToImmutableList();

            if (fromPacks.Any(p => Path.GetFileName(p).Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase)))
            {
                return fromPacks.Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableList();
            }

            return RunningRuntime();
        }

        /// <summary>
        /// The source generators the shared frameworks ship next to their reference assemblies, like the
        /// regex and the JSON generator, in <c>packs/Microsoft.NETCore.App.Ref/10.0.0/analyzers/dotnet/cs</c>.
        /// </summary>
        internal static ImmutableList<string> AnalyzersFor(string? targetFramework, ImmutableList<string> frameworkReferences)
        {
            var frameworks = frameworkReferences.Contains("Microsoft.NETCore.App")
                                 ? frameworkReferences
                                 : frameworkReferences.Insert(0, "Microsoft.NETCore.App");

            return RefDirectoriesOf(BaseTfm(targetFramework), frameworks)
                   .Select(d => Path.GetFullPath(Path.Combine(d, "..", "..", "analyzers", "dotnet", "cs")))
                   .Where(Directory.Exists)
                   .SelectMany(d => Directory.EnumerateFiles(d, "*.dll"))
                   .ToImmutableList();
        }

        /// <summary>The assemblies the running process trusts, which for a .NET test run is the shared framework plus its dependencies.</summary>
        internal static ImmutableList<string> RunningRuntime()
        {
            var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;

            return trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                          .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                          .ToImmutableList();
        }

        private static ImmutableList<string> RefDirectoriesOf(string? tfm, ImmutableList<string> frameworks)
        {
            if (tfm is null)
            {
                return ImmutableList<string>.Empty;
            }

            return frameworks.Select(PackOf)
                             .Distinct(StringComparer.Ordinal)
                             .Select(f => RefDirectories.GetOrAdd((f, tfm), key => FindRefDirectory(key.Framework, key.Tfm)))
                             .Where(d => d is not null)
                             .Select(d => d!)
                             .ToImmutableList();
        }

        /// <summary>
        /// "Microsoft.WindowsDesktop.App.WPF" and ".WindowsForms" are profiles of one pack; referencing
        /// the whole pack is a superset, which binding does not mind.
        /// </summary>
        private static string PackOf(string frameworkReference)
        {
            return frameworkReference.StartsWith("Microsoft.WindowsDesktop.App", StringComparison.Ordinal)
                       ? "Microsoft.WindowsDesktop.App"
                       : frameworkReference;
        }

        private static string? FindRefDirectory(string framework, string tfm)
        {
            var packRoot = Path.Combine(DotnetRoot(), "packs", framework + ".Ref");

            if (!Directory.Exists(packRoot))
            {
                return null;
            }

            return Directory.EnumerateDirectories(packRoot)
                                        .Select(versionDirectory => (Version: ParseVersion(Path.GetFileName(versionDirectory)),
                                                                     Directory: Path.Combine(versionDirectory, "ref", tfm)))
                                        .Where(v => v.Version is not null && Directory.Exists(v.Directory))
                                        .OrderByDescending(v => v.Version)
                                        .Select(v => v.Directory)
                                        .FirstOrDefault();
        }

        /// <summary>The installation the running runtime belongs to: <c>dotnet/shared/Microsoft.NETCore.App/10.0.x</c> sits three levels below it.</summary>
        private static string DotnetRoot()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            if (!string.IsNullOrWhiteSpace(fromEnvironment) && Directory.Exists(Path.Combine(fromEnvironment, "packs")))
            {
                return fromEnvironment;
            }

            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            return Path.GetFullPath(Path.Combine(runtimeDirectory, "..", "..", ".."));
        }

        /// <summary>"net10.0-windows7.0" compiles against the "net10.0" reference pack; "netstandard2.0" and the like have none.</summary>
        private static string? BaseTfm(string? targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                return null;
            }

            var tfm = targetFramework.Split('-')[0];

            return tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase) && tfm.Contains('.', StringComparison.Ordinal) ? tfm : null;
        }

        private static Version? ParseVersion(string text)
        {
            // Previews look like "10.0.0-rc.1.25451.107"; the numeric part orders them well enough.
            var numeric = text.Split('-')[0];

            return Version.TryParse(numeric, out var version) ? version : null;
        }
    }
}
