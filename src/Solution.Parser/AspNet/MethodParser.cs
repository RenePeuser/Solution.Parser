using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    internal static class AddMethodParserExtension
    {
        internal static void AddMethodParser(this IServiceCollection services)
        {
            services.AddDataTypeFinder();
            services.AddUrlBuilder();
            services.AddModelNormalizer();
            services.AddParameterNormalizer();
            services.AddResponseTypeNormalizer();

            services.AddSingletonIfNotExists<MethodParser>();
        }
    }

    internal sealed class MethodParser
    {
        private readonly DataTypeFinder _dataTypeFinder;
        private readonly UrlBuilder _urlBuilder;
        private readonly ModelNormalizer _modelNormalizer;
        private readonly ParameterNormalizer _parameterNormalizer;
        private readonly ResponseTypeNormalizer _responseTypeNormalizer;

        public MethodParser(DataTypeFinder dataTypeFinder,
                            UrlBuilder urlBuilder,
                            ModelNormalizer modelNormalizer,
                            ParameterNormalizer parameterNormalizer,
                            ResponseTypeNormalizer responseTypeNormalizer)
        {
            _dataTypeFinder = dataTypeFinder;
            _urlBuilder = urlBuilder;
            _modelNormalizer = modelNormalizer;
            _parameterNormalizer = parameterNormalizer;
            _responseTypeNormalizer = responseTypeNormalizer;
        }

        internal IImmutableList<MethodInfos> Parse(IImmutableList<Method> methods,
                                                   IImmutableList<string> baseUrls,
                                                   IImmutableList<MethodInfo> reflectionTypes,
                                                   IImmutableList<CSharpSyntaxTree> syntaxTrees)
        {
            // Only methods which represents a http endpoint
            var publicMethods = methods.Where(m => m.Attributes.Any(attribute => attribute.Name.StartWith("Http"))).ToImmutableList();
            return publicMethods.Select(method => Parse(method, baseUrls, reflectionTypes, syntaxTrees)).ToImmutableList();
        }

        private MethodInfos Parse(Method method,
                                  IImmutableList<string> baseUrls,
                                  IImmutableList<MethodInfo> reflectionTypes,
                                  IImmutableList<CSharpSyntaxTree> syntaxTrees)
        {

            IImmutableList<MethodInfo> methods = reflectionTypes.Where(m => m.Name == method.Name &&
                                                     m.GetParameters().Length == method.Parameters.Count).ToImmutableList();

            // Simple workaround fallback -> this is a bug in the code method name and parameter same !
            if (methods.Count > 1)
            {
                methods = FilterByReturnType(methods, method);
            }

            if (methods.Count != 1)
            {
                throw new InvalidOperationException($"Unexpected method count for method name: {method.Name}. Please go sure your current code state match the assemblies in your project. Please try to rebuild the solution before you run the client gen again");
            }

            var methodReflection = methods[0];
            var httpAttribute = method.Attributes.FirstOrDefault(a => a.Name.StartWith("Http"));
            var httpAction = httpAttribute?.Name.Replace("Http", string.Empty) ?? string.Empty;
            var swaggerOperation = method.Attributes.FirstOrDefault(a => a.Name == "SwaggerOperation");
            var swaggerOperationId = swaggerOperation?.Arguments.FirstOrDefault(a => a.Contains("OperationId"))?.Split("=").Last().Replace(@"""", string.Empty)?.Trim() ?? string.Empty;
            var produceResponseType = GetProduceResponseType(method);
            var relativeUrl = _urlBuilder.BuildFrom(baseUrls, method);
            var reflectionParameters = methodReflection.GetParameters().ToImmutableList();
            var allModels = _dataTypeFinder.FindDataType(methodReflection, syntaxTrees).ToImmutableList();
            var parameters = _parameterNormalizer.Normalize(method, reflectionParameters, allModels);
            var normalizeDataTypes = _modelNormalizer.Normalize(allModels).ToImmutableList();
            var responseType = _responseTypeNormalizer.GetResponseType(methodReflection, method, normalizeDataTypes);

            return new MethodInfos()
            {
                Name = method.Name,
                SwaggerOperationId = swaggerOperationId,
                HttpAction = httpAction,
                ProduceResponseTypes = produceResponseType,
                RelativeUrls = relativeUrl,
                Parameters = parameters,
                ResponseType = responseType,
                RequestType = null, // ToDo not sure how realy to detect ot many possibilities here,
                Attributes = method.Attributes,
                Models = normalizeDataTypes
            };
        }

        private static IImmutableList<ProduceResponseTypes> GetProduceResponseType(Method method)
        {
            var produceResponseType = method.Attributes.Where(attribute => attribute.Name == "ProducesResponseType")
                                            .Select(produce =>
                                            {
                                                var type = produce.Arguments.FirstOrDefault() ?? string.Empty;
                                                var code = produce.Arguments.LastOrDefault()?.ToIntOrDefault() ?? 0;
                                                return new ProduceResponseTypes(type, code);
                                            }).ToImmutableList();
            return produceResponseType;
        }


        private IImmutableList<MethodInfo> FilterByReturnType(IImmutableList<MethodInfo> methods, Method method)
        {
            var filteredMethods = methods.Where(m =>
            {
                var returnTypes = m.ReturnType.GetAllGenericArguments();
                return returnTypes.Any(t => method.ReturnParameter.Contains(t.Name));
            }).ToImmutableList();


            return filteredMethods;
        }
    }
}
