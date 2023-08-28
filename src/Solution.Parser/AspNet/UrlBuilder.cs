using System.Collections.Immutable;
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

        internal IImmutableList<string> BuildFrom(IImmutableList<string> baseUrls, Method method)
        {
            var immutableListBuilder = ImmutableList.CreateBuilder<string>();

            // 1. Iterate over all base urls
            foreach (var baseUrl in baseUrls)
            {
                // 2. Iterate over all http methods
                var httpMethods = method.Attributes.Where(a => a.Name.StartWith("Http")).ToImmutableList();
                foreach (var httpMethod in httpMethods)
                {
                    var httpMethodRoute = httpMethod.Arguments.FirstOrDefault()?.Trim('"') ?? string.Empty;

                    // 3. Iterate over all routes
                    var routes = method.Attributes.Where(a => a.Name == "Route").Select(a => a.Arguments.FirstOrDefault()?.Trim('"')).FilterNullObjects().ToImmutableList();

                    if (routes.IsEmpty())
                    {
                        var urlWithParams = BuildUrlInternal(method, baseUrl, string.Empty, httpMethodRoute);
                        immutableListBuilder.Add(urlWithParams);

                        continue;
                    }

                    foreach (var route in routes)
                    {
                        var urlWithParams = BuildUrlInternal(method, baseUrl, route, httpMethodRoute);
                        immutableListBuilder.Add(urlWithParams);
                    }
                }

            }

            return immutableListBuilder.ToImmutable();
        }

        private string BuildUrlInternal(Method method, string baseUrl, string route, string httpMethodTemplate)
        {
            var relativeUrl = route.IsNullOrWhiteSpace() ?
                                    $"{baseUrl.TrimEnd('/')}/{httpMethodTemplate.TrimStart('/')}" :
                                    $"{baseUrl.TrimEnd('/')}/{route}/{httpMethodTemplate.TrimStart('/')}";

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
