using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.Solution;

namespace Solution.Parser.AspNet
{
    public static class AddApiTypeLoaderExtension
    {
        public static void AddApiTypeLoader(this IServiceCollection services)
        {
            services.AddAssemblyTypeLoader();

            services.AddSingletonIfNotExists<IApiTypeLoader, ApiTypeLoader>();
            services.AddSingletonIfNotExists<ApiTypeLoader>();
        }
    }

    public interface IApiTypeLoader
    {
        IImmutableList<Type> GetAllTypesFrom(SolutionFile parsedSolution);
        IImmutableList<Type> GetAllTypesFrom(SolutionFile parsedSolution, DirectoryInfo assemblyDirectory);
    }

    internal sealed class ApiTypeLoader : IApiTypeLoader
    {
        private readonly AssemblyTypeLoader _assemblyTypeLoader;

        public ApiTypeLoader(AssemblyTypeLoader assemblyTypeLoader)
        {
            _assemblyTypeLoader = assemblyTypeLoader;
        }

        public IImmutableList<Type> GetAllTypesFrom(SolutionFile parsedSolution, DirectoryInfo assemblyDirectory)
        {
            var webAppProject = parsedSolution.ProductiveProjects.FirstOrDefault(p => p.Document.ToString().Contains("Sdk=\"Microsoft.NET.Sdk.Web\""));
            if (webAppProject.IsNull())
            {
                throw new InvalidOperationException($"Your solution: {parsedSolution.SolutionFileInfo.Value} does not contain a project which is defined as Sdk=\"Microsoft.NET.Sdk.Web\"");
            }

            var searchPattern = $"{webAppProject.ProjectFileInfo.FileNameWithoutExtenion}.dll";
            var assembly = assemblyDirectory.EnumerateFiles(searchPattern, SearchOption.AllDirectories).FirstOrDefault();
            if (assembly.IsNull())
            {
                throw new InvalidOperationException($"Your project: '{webAppProject.ProjectFileInfo.Value.FullName}' path does not contain the matching assembly: '{assembly}'. Please build your project or solution before you want to create a client from");
            }

            // Get all types which are declared in the API assembly - Need to unique ident the types for client generation.
            var types = _assemblyTypeLoader.GetAllTypesFrom(assembly);

            return types;
        }

        public IImmutableList<Type> GetAllTypesFrom(SolutionFile parsedSolution)
        {
            var webAppProject = parsedSolution.ProductiveProjects.FirstOrDefault(p => p.Document.ToString().Contains("Sdk=\"Microsoft.NET.Sdk.Web\""));
            if (webAppProject.IsNull())
            {
                throw new InvalidOperationException($"Your solution: {parsedSolution.SolutionFileInfo.Value} does not contain a project which is defined as Sdk=\"Microsoft.NET.Sdk.Web\"");
            }

            var searchPattern = $"{webAppProject.ProjectFileInfo.FileNameWithoutExtenion}.dll";
            var assembly = webAppProject.ProjectFileInfo.Value.Directory!.EnumerateFiles(searchPattern, SearchOption.AllDirectories).FirstOrDefault(file => file.FullName.Contains("Debug") && file.FullName.Contains("net7") && !file.FullName.Contains("obj"));
            if (assembly.IsNull())
            {
                throw new InvalidOperationException($"Your project: '{webAppProject.ProjectFileInfo.Value.FullName}' path does not contain the matching assembly: '{assembly}'. Please build your project or solution before you want to create a client from");
            }

            // Get all types which are declared in the API assembly - Need to unique ident the types for client generation.
            var types = _assemblyTypeLoader.GetAllTypesFrom(assembly);

            return types;
        }
    }
}
