using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Configuration;

namespace Solution.Parser.AspNet
{
    internal static class AddAssemblyTypeLoaderExtension
    {
        internal static void AddAssemblyTypeLoader(this IServiceCollection services)
        {
            services.AddSingletonIfNotExists<AssemblyTypeLoader>();
        }
    }

    internal sealed class AssemblyTypeLoader
    {
        private static readonly Lazy<string[]> RuntimeAssemblies = new(() => Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));

        private static readonly Lazy<string> GlobalPackagesFolder = new(() => SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null)));

        // A name that could not be found is asked for again and again; the search behind it walks the whole package folder.
        private static readonly ConcurrentDictionary<string, bool> NotFound = new(StringComparer.OrdinalIgnoreCase);

        private static int _resolverSubscribed;

        internal ImmutableList<Type> GetAllTypesFrom(FileInfo assemblyFile)
        {
            var types = GetAllTypes(assemblyFile);
            var additionalTypes = GetDeepTypeInfos(types).DistinctBy(type => type.FullName);

            var allTypes = types.Concat(additionalTypes).ToImmutableList();

            var allDeclaredTypes = allTypes.DistinctBy(type => type.FullName).ToImmutableList();
            // now we have to split of all generic types as well
            // var allTypes = allDeclaredTypes.SelectMany(type => type.GetAllTypesFromGenericType()).DistinctBy(t => t.FullName).ToImmutableList();

            return allDeclaredTypes;


        }
        private ImmutableList<Type> GetAllTypes(FileInfo assemblyFile)
        {
            Directory.SetCurrentDirectory(assemblyFile.Directory!.FullName);

            // Once per process: every subscription runs on every miss, and the resolver is static anyway.
            if (Interlocked.Exchange(ref _resolverSubscribed, 1) == 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }

            var assembly = Assembly.LoadFrom(assemblyFile.FullName);
            var types = assembly.GetTypes().ToImmutableList();
            return types;
        }


        private IEnumerable<Type> GetDeepTypeInfos(ImmutableList<Type> types)
        {
            foreach (var type in types)
            {
                if (type.DeclaringType.IsNull())
                {
                    continue;
                }

                if (type.Name.StartWith("<"))
                {
                    continue;
                }

                yield return type;

                var genericTypes = type.GetAllTypesFromGenericType().DistinctBy(t => t.FullName);
                foreach (var genericType in genericTypes)
                {
                    yield return genericType;
                }

                var methods = type.GetMethods();
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        yield return parameter.ParameterType;
                    }

                    yield return method.ReturnType;
                }
            }
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var strings = args.Name.Split(',');
            var assemblyName = strings.First();
            var searchPattern = $"{assemblyName}.dll";

            // 1. Check first if it is already loaded in current app domain
            var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName()!.Name.EqualsTo(assemblyName));
            if (alreadyLoaded.IsNotNull())
            {
                return alreadyLoaded;
            }

            // 2. Check current directory
            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            var dll = currentDirectory.EnumerateFiles(searchPattern).FirstOrDefault();
            if (dll.IsNotNull())
            {
                return Assembly.LoadFrom(dll.FullName);
            }

            if (NotFound.ContainsKey(assemblyName))
            {
                return null;
            }

            // 3. Search in the runtime folder
            var foundInRuntimeFolder = RuntimeAssemblies.Value.FirstOrDefault(file => file.Contains(searchPattern));
            if (foundInRuntimeFolder.IsNotNullOrWhiteSpace())
            {
                return Assembly.LoadFrom(foundInRuntimeFolder);
            }

            // 4. If not found search in nuget packages
            //    Very tricky here
            var missingPackage = FindInPackages(assemblyName, searchPattern);
            if (missingPackage.IsEmpty())
            {
                NotFound.TryAdd(assemblyName, true);

                return null;
            }

            var fileToLoad = missingPackage.Last().FullName;

            try
            {
                return Assembly.LoadFrom(fileToLoad);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                NotFound.TryAdd(assemblyName, true);

                return null;
            }
        }

        /// <summary>
        /// The package of the same name first, which is where an assembly almost always lives; the whole
        /// package folder, often a hundred thousand files, only when that has nothing.
        /// </summary>
        private static List<FileInfo> FindInPackages(string assemblyName, string searchPattern)
        {
            var nugetDirectory = new DirectoryInfo(GlobalPackagesFolder.Value);

            if (!nugetDirectory.Exists)
            {
                return [];
            }

            var packageDirectory = new DirectoryInfo(Path.Combine(nugetDirectory.FullName, assemblyName.ToLowerInvariant()));

            if (packageDirectory.Exists)
            {
                var inPackage = packageDirectory.EnumerateFiles(searchPattern, SearchOption.AllDirectories).ToList();

                if (inPackage.Count > 0)
                {
                    return inPackage;
                }
            }

            return nugetDirectory.EnumerateFiles(searchPattern, SearchOption.AllDirectories)
                                 .Where(file => file.Directory!.FullName.Contains(assemblyName, StringComparison.OrdinalIgnoreCase))
                                 .ToList();
        }
    }

    internal static class TypeExtensions
    {
        internal static IEnumerable<Type> GetAllGenericArguments(this Type type)
        {
            var genericArguments = type.GetGenericArguments();
            foreach (var genericArgument in genericArguments)
            {
                var subTypes = GetAllGenericArguments(genericArgument);
                foreach (var subType in subTypes)
                {
                    yield return subType;
                }

                yield return genericArgument;
            }
        }

        internal static IEnumerable<Type> GetAllTypesFromGenericType(this Type type)
        {
            if (type.IsGenericType.IsFalse())
            {
                yield return type;
            }

            var genericArguments = type.GetGenericArguments();
            foreach (var genericArgument in genericArguments)
            {
                yield return genericArgument;
            }
        }
    }
}
