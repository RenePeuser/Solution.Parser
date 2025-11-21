using System.Collections.Immutable;
using System.Linq;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    internal static class AddApiVersionFinderExtension
    {
        internal static void AddApiVersionFinder(this IServiceCollection services)
        {
            services.AddSingletonIfNotExists<ApiVersionFinder>();
        }
    }

    public class ApiVersionFinder
    {
        public ImmutableList<ApiVersion> FindAllApiVersions(ImmutableList<CSharpSyntaxTree> syntaxTrees)
        {
            var controllers = (from syntaxTree in syntaxTrees
                               from @class in syntaxTree.Classes
                               where @class.BaseTypes.Any(baseType => baseType.TypeName.Contains("Controller")) // ODataController, Controller, ControllerBase
                               select @class).ToImmutableList();

            var allVersions = controllers.SelectMany(controller => controller.Attributes.Where(a => a.Name.EqualsTo("ApiVersion")))
                                         .Where(a => a.Arguments.Any())
                                         .Select(a => a.Arguments[0].Replace("\"", string.Empty))
                                         .Distinct()
                                         .Select(version => version.ToApiVersion());

            var orderedVersions = allVersions.OrderBy(v => v.Major).ThenBy(v => v.Minor).ToImmutableList();
            return orderedVersions;
        }
    }

    public record ApiVersion
    {
        public required int Major { get; init; } = 1;
        public int Minor { get; init; }
    }

    internal static class ApiVersionExtensions
    {
        internal static ApiVersion ToApiVersion(this string version)
        {
            if (version.IsNullOrWhiteSpace())
            {
                return new ApiVersion
                {
                    Major = 1
                };
            }

            var splittedString = version.Split(".");
            if (int.TryParse(splittedString[0], out var major))
            {
                return new ApiVersion()
                {
                    Major = major
                };
            }

            return new ApiVersion()
            {
                Major = 1
            };
        }

        internal static string GetVersionForCode(this ApiVersion apiVersion)
        {
            return $"V{apiVersion.Major}";
        }
    }
}
