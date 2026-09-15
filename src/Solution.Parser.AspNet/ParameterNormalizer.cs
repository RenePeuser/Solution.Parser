using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    internal static class AddParameterNormalizerExtension
    {
        internal static void AddParameterNormalizer(this IServiceCollection services)
        {
            services.AddSingletonIfNotExists<ParameterNormalizer>();
        }
    }

    internal sealed class ParameterNormalizer
    {
        internal ImmutableList<Parameter> Normalize(Method method, ImmutableList<ParameterInfo> reflectionParameters, ImmutableList<DeclarationToType> allModels)
        {
            return method.Parameters.Select(p =>
            {
                // NEW: we have to trim @ if exists cause alias or conflicting reserved names which
                //      escaped with @ have to be handled
                var reflectionParamer = reflectionParameters.First(rp => rp.Name.EqualsTo(p.Name.TrimStart('@')));

                // If type is system type like string, int nothing to do
                if (reflectionParamer.ParameterType.IsSystemType())
                {
                    return p;
                }

                if (reflectionParamer.ParameterType.FullName!.Contains("System.") || reflectionParamer.ParameterType.FullName.Contains("Microsoft."))
                {
                    return p;
                }

                // 2. If the type is a model which was declared we use the simplified type name
                //    Important dependent on nullable declaration ? we have to ignore ?
                if (allModels.Any(type => type.Declaration.Name.EqualsTo(p.Type.TrimEnd('?'))))
                {
                    return p;
                }

                // New: If someone use a full qualified name at parameter level which will be copied as data type into client
                //      this have to be replaced by the new name of the copy
                var model = allModels.FirstOrDefault(m => m.Declaration.FullQualifiedName.EqualsTo(p.Type));
                if (model.IsNotNull())
                {
                    return p with { Type = model.Declaration.Name };
                }


                // Sample: Pulse.Common.Data.V1.User then we need full qualified names !
                return p with { Type = reflectionParamer.ParameterType.FullName! };

            }).ToImmutableList();
        }
    }
}
