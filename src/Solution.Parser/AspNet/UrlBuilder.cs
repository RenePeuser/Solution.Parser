using System.Linq;
using System.Text.RegularExpressions;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    internal static class AddUrlBuilderExtension
    {
        internal static void AddUrlBuilder(this IServiceCollection services)
        {
            services.AddQueryBuilder();

            services.AddSingletonIfNotExists<UrlBuilder>();
        }
    }

    // What we create here:
    // - We create an url with all needed parameters
    // 
    // Samples:
    //
    // core/alive
    // 
    // v1.0/project/{projectId}/rowlevelsecurity/{rowLevelSecurityId}/user/{userId}?useCache={useCache}
    // v2.0/project/{projectId}/resource/{resourceId}/parent/{resourceParentId}?useCache={useCache}
    internal sealed partial class UrlBuilder
    {
        private readonly QueryBuilder _queryBuilder;
        private readonly Regex _parameterRegEx = ParamaterRegEx();

        public UrlBuilder(QueryBuilder queryBuilder)
        {
            _queryBuilder = queryBuilder;
        }

        internal string BuildFrom(string baseUrl, Method method)
        {
            var httpAttribute = method.Attributes.FirstOrDefault(a => a.Name.StartWith("Http"));
            var httpMethodRoute = httpAttribute?.Arguments.FirstOrDefault()?.Trim('"') ?? string.Empty;
            var relativeUrl = $"{baseUrl.TrimEnd('/')}/{httpMethodRoute.TrimStart('/')}";

            var routeOnMethod = method.Attributes.FirstOrDefault(a => a.Name == "Route")?.Arguments.FirstOrDefault()?.Trim('"');
            if (routeOnMethod.IsNotNullOrWhiteSpace())
            {
                relativeUrl = $"{baseUrl.TrimEnd('/')}/{routeOnMethod}/{httpMethodRoute.TrimStart('/')}";
            }

            var urlParameters = _parameterRegEx.Matches(relativeUrl).Select(m => m.Value);
            urlParameters.ForEach(param =>
            {
                var normalizedParameter = param.FirstCharToLower();
                relativeUrl = relativeUrl.Replace(param, normalizedParameter);
            });

            // Specific file parameter notation have to be replaced too.
            relativeUrl = relativeUrl.Replace("**", string.Empty);

            var parameters = _queryBuilder.BuildFrom(method);

            var urlWithParams = $"{relativeUrl.TrimEnd('/')}{parameters}";

            return urlWithParams;
        }

        [GeneratedRegex("(?<=\\{).+?(?=\\})")]
        private static partial Regex ParamaterRegEx();
    }
}
