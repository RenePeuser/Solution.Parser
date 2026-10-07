using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Solution.Parser.CSharp
{
    /// <summary>A compile time assembly of a NuGet package, as restore resolved it.</summary>
    internal sealed record PackageAssembly(string PackageId, string Version, string Path);

    /// <summary>
    /// What <c>dotnet restore</c> wrote to <c>obj/project.assets.json</c>: the package assemblies to
    /// compile against, transitive ones included, and the shared frameworks the project needs.
    /// </summary>
    /// <remarks>
    /// Reading the restore result instead of the PackageReferences of the csproj is what makes this
    /// correct: versions are resolved, transitive packages are listed and the assembly matching the
    /// target framework is already picked.
    /// </remarks>
    internal sealed record AssetsFile(string TargetFramework,
                                      ImmutableList<PackageAssembly> Packages,
                                      ImmutableList<string> Analyzers,
                                      ImmutableList<string> FrameworkReferences)
    {
        internal static AssetsFile? TryRead(string projectDirectory)
        {
            var path = System.IO.Path.Combine(projectDirectory, "obj", "project.assets.json");

            if (!File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);

            return Read(document.RootElement);
        }

        private static AssetsFile? Read(JsonElement root)
        {
            // The first target without a runtime identifier: "net10.0" rather than "net10.0/win-x64".
            var target = root.GetProperty("targets").EnumerateObject().FirstOrDefault(t => !t.Name.Contains('/', StringComparison.Ordinal));

            if (target.Value.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var packageFolders = root.TryGetProperty("packageFolders", out var folders)
                                     ? folders.EnumerateObject().Select(f => f.Name).ToImmutableList()
                                     : ImmutableList<string>.Empty;

            var libraries = root.GetProperty("libraries");
            var packages = ImmutableList.CreateBuilder<PackageAssembly>();
            var analyzers = ImmutableList.CreateBuilder<string>();
            var frameworkReferences = ImmutableSortedSet.CreateBuilder<string>(StringComparer.Ordinal);

            foreach (var library in target.Value.EnumerateObject())
            {
                if (library.Value.TryGetProperty("frameworkReferences", out var libraryFrameworks))
                {
                    frameworkReferences.UnionWith(libraryFrameworks.EnumerateArray().Select(f => f.GetString()!));
                }

                // A package without compile assets may still bring source generators, so compile is optional.
                if (library.Value.GetProperty("type").GetString() != "package" ||
                    !libraries.TryGetProperty(library.Name, out var libraryInfo) ||
                    !libraryInfo.TryGetProperty("path", out var libraryPath))
                {
                    continue;
                }

                var (id, version) = SplitName(library.Name);
                var packageDirectory = packageFolders.Select(folder => System.IO.Path.Combine(folder, libraryPath.GetString()!))
                                                     .FirstOrDefault(Directory.Exists);

                var files = libraryInfo.TryGetProperty("files", out var fileList)
                                ? fileList.EnumerateArray().Select(f => f.GetString()!).ToImmutableList()
                                : ImmutableList<string>.Empty;

                if (packageDirectory is null)
                {
                    continue;
                }

                analyzers.AddRange(SourceGenerators.OfPackage(packageDirectory, files));

                var compileAssets = library.Value.TryGetProperty("compile", out var compile)
                                        ? compile.EnumerateObject().Select(c => c.Name)
                                        : [];

                foreach (var assembly in compileAssets.Concat(BuildReferences(files, target.Name)))
                {
                    // "_._" marks a package that deliberately contributes nothing for this framework.
                    if (assembly.EndsWith("_._", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var file = System.IO.Path.Combine(packageDirectory, assembly);

                    if (File.Exists(file))
                    {
                        packages.Add(new PackageAssembly(id, version, System.IO.Path.GetFullPath(file)));
                    }
                }
            }

            frameworkReferences.UnionWith(ProjectFrameworkReferences(root, target.Name));

            return new AssetsFile(target.Name, packages.ToImmutable(), analyzers.ToImmutable(), frameworkReferences.ToImmutableList());
        }

        /// <summary>
        /// Assemblies a package adds through its MSBuild targets rather than through lib, the way
        /// MSTest.TestFramework adds the assembly holding <c>TestContext</c> from
        /// <c>buildTransitive/net9.0</c>. Without evaluating the targets the best guess is: the dlls
        /// right inside the nearest compatible framework folder of build or buildTransitive.
        /// </summary>
        private static ImmutableList<string> BuildReferences(ImmutableList<string> files, string targetFramework)
        {
            var target = NetVersion(targetFramework.Split('-')[0]);

            if (target is null)
            {
                return ImmutableList<string>.Empty;
            }

            var candidates = files.Select(f => f.Split('/'))
                                  .Where(parts => parts.Length == 3
                                                  && parts[0] is "build" or "buildTransitive"
                                                  && parts[2].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                  .Select(parts => (File: string.Join('/', parts), Version: NetVersion(parts[1])))
                                  .Where(c => c.Version is not null && c.Version <= target)
                                  .ToList();

            var best = candidates.Select(c => c.Version).Max();

            return candidates.Where(c => c.Version == best).Select(c => c.File).ToImmutableList();
        }

        /// <summary>"net9.0" is 9.0; "netstandard2.0", "net462" and friends are not a .NET folder this cares about.</summary>
        private static Version? NetVersion(string tfm)
        {
            return tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase)
                   && !tfm.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
                   && !tfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase)
                   && tfm.Contains('.', StringComparison.Ordinal)
                   && Version.TryParse(tfm[3..], out var version)
                       ? version
                       : null;
        }

        private static ImmutableList<string> ProjectFrameworkReferences(JsonElement root, string targetFramework)
        {
            if (!root.TryGetProperty("project", out var project) ||
                !project.TryGetProperty("frameworks", out var frameworks))
            {
                return ImmutableList<string>.Empty;
            }

            // The key is the alias from the csproj ("net10.0-windows"), the target name the normalized
            // one ("net10.0-windows7.0"), so match on the "framework" value as well.
            var framework = frameworks.EnumerateObject()
                                      .FirstOrDefault(f => targetFramework.StartsWith(f.Name, StringComparison.OrdinalIgnoreCase));

            if (framework.Value.ValueKind != JsonValueKind.Object ||
                !framework.Value.TryGetProperty("frameworkReferences", out var references))
            {
                return ImmutableList<string>.Empty;
            }

            return references.EnumerateObject().Select(r => r.Name).ToImmutableList();
        }

        private static (string Id, string Version) SplitName(string name)
        {
            var slash = name.IndexOf('/', StringComparison.Ordinal);

            return slash < 0 ? (name, string.Empty) : (name[..slash], name[(slash + 1)..]);
        }
    }
}
