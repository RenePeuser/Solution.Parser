using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    internal static class AddResponseTypeNormalizerExtension
    {
        internal static void AddResponseTypeNormalizer(this IServiceCollection services)
        {
            services.AddSingletonIfNotExists<ResponseTypeNormalizer>();
        }
    }

    internal sealed class ResponseTypeNormalizer
    {
        internal ResponseType GetResponseType(MethodInfo methodInfo,
                                              Method method,
                                              ImmutableList<DeclarationBase> declarationBases)
        {

            // 0. Get correct types
            var returnType = methodInfo.ReturnType.IsGenericType ? methodInfo.ReturnType.GetGenericArguments().First() : methodInfo.ReturnType;

            // 1. If the type is a pure system type we take it
            if (returnType.IsSystemType() ||
                returnType.FullName!.Contains("System.") || returnType.FullName!.Contains("Microsoft."))
            {
                return new ResponseType(method.ReturnParameter, returnType.Name);
            }

            // 2. If the type is a model which was declared we use the simplified type name
            if (declarationBases.Any(type => type.Name.EqualsTo(returnType.Name)))
            {
                return new ResponseType(method.ReturnParameter, returnType.Name);
            }

            // 3. External type we need full qualified name
            // Sample: Pulse.Common.Data.V1.User then we need full qualified names !
            var returnTypeWithTask = method.ReturnParameter.Contains(returnType.FullName!) ? method.ReturnParameter : method.ReturnParameter.Replace(returnType.Name, returnType.FullName);
            return new ResponseType(returnTypeWithTask, returnType.FullName!);
        }
    }
}
