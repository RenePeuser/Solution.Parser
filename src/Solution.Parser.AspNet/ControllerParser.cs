using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    public static class AddControllerParserExtension
    {
        public static void AddControllerParser(this IServiceCollection services)
        {
            services.AddMethodParser();

            services.AddSingletonIfNotExists<IControllerParser, ControllerParser>();
            services.AddSingletonIfNotExists<ControllerParser>();
        }
    }


    public interface IControllerParser
    {
        ImmutableList<ControllerInfo> ExtractFrom(ImmutableList<CSharpSyntaxTree> syntaxTrees,
                                                   ImmutableList<Type> reflectionTypes);
    }

    internal sealed class ControllerParser(MethodParser methodParser) : IControllerParser
    {
        public ImmutableList<ControllerInfo> ExtractFrom(ImmutableList<CSharpSyntaxTree> syntaxTrees,
                                                          ImmutableList<Type> reflectionTypes)
        {
            // 1. Detect all controllers. Derived class like Controller, ControllerBase, ODataController and so on
            var controllers = syntaxTrees.GetAllControllers();

            // 2. Extract all needed infos for generation out
            var controllerInfos = Parse(controllers, reflectionTypes, syntaxTrees).ToImmutableList();

            return controllerInfos;
        }

        private IEnumerable<ControllerInfo> Parse(ImmutableList<Class> controllers,
                                                  ImmutableList<Type> reflectionTypes,
                                                  ImmutableList<CSharpSyntaxTree> syntaxTrees)
        {
            foreach (var controller in controllers)
            {
                // 0. Reflection controller
                //    This is the case if an alive controller comes from another lib
                var controllerType = reflectionTypes.FirstOrDefault(type => type.FullName.EqualsTo(controller.FullQualifiedName));
                if (controllerType.IsNull())
                {
                    continue;
                }

                // 1. Extract meta infos version, base url and son on.
                var version = controller.Attributes.FirstOrDefault(a => a.Name.EqualsTo("ApiVersion"))?.Arguments.FirstOrDefault()?.Trim('"') ?? "1.0";
                var normalizedVersion = $"V{version.Replace(".0", string.Empty) // V1.0 => V1
                                                   .Replace(".", "_")}"; // V1.1 => V1_1}"

                // 2. Get all declared base routes
                var baseUrls = controller.Attributes.Where(a => a.Name.EqualsTo("Route")).Select(a => a.Arguments.FirstOrDefault()?.Replace("{version:apiVersion}", version.ToLowerInvariant()).Trim('"') ?? string.Empty).ToImmutableList();

                // 3. Now parse all methods
                var methodReflection = controllerType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).ToImmutableList();
                var methods = methodParser.Parse(controller.Methods, baseUrls, methodReflection, syntaxTrees);

                // 4. Return controller infos
                yield return new ControllerInfo
                {
                    Name = controller.Name,
                    FilePath = controller.FilePath,
                    DomainName = controller.Name.Replace("Controller", string.Empty),
                    Version = new VersionInfo(version, normalizedVersion),
                    BaseUrls = baseUrls,
                    Methods = methods,
                    Attributes = controller.Attributes
                };
            }
        }
    }

    internal static class SyntaxTreeExtensions
    {
        internal static ImmutableList<Class> GetAllControllers(this ImmutableList<CSharpSyntaxTree> syntaxTrees)
        {
            var controllers = (from syntaxTree in syntaxTrees
                               from @class in syntaxTree.Classes
                               where @class.BaseTypes.Any(baseType => baseType.TypeName.Contains("Controller", StringComparison.OrdinalIgnoreCase))
                               select @class).ToImmutableList();

            return controllers;
        }


        internal static (DeclarationBase? Declaration, Type Type) FindDataType(this ImmutableList<CSharpSyntaxTree> syntaxTrees, Type reflectionType)
        {
            var fullqualifiedName = reflectionType.FullName?.Replace("+", "."); // Nested classes have + in reflection full name as separator !
            if (fullqualifiedName.IsNull())
            {
                return (null, reflectionType);
            }

            // Generic types have to be simplified against generic types of solution parser
            fullqualifiedName = fullqualifiedName.Split("`").First();

            // 2.1 Check find class first
            var @class = syntaxTrees.SelectMany(tree => tree.Classes).FirstOrDefault(c => c.FullQualifiedName.EqualsTo(fullqualifiedName));
            if (@class.IsNotNull())
            {
                return (@class, reflectionType);
            }

            // 2.2. Check records
            var @record = syntaxTrees.SelectMany(tree => tree.Records).FirstOrDefault(c => c.FullQualifiedName.EqualsTo(fullqualifiedName));
            if (@record.IsNotNull())
            {
                return (@record, reflectionType);
            }

            // 2.3. Check enum
            var @enum = syntaxTrees.SelectMany(tree => tree.Enums).FirstOrDefault(c => c.FullQualifiedName.EqualsTo(fullqualifiedName));
            if (@enum.IsNotNull())
            {
                return (@enum, reflectionType);
            }

            // 2.3. Check interface
            var @interface = syntaxTrees.SelectMany(tree => tree.Interfaces).FirstOrDefault(c => c.FullQualifiedName.EqualsTo(fullqualifiedName));
            if (@interface.IsNotNull())
            {
                return (@interface, reflectionType);
            }

            // 2.4. Check interface
            var @stuct = syntaxTrees.SelectMany(tree => tree.Structs).FirstOrDefault(c => c.FullQualifiedName.EqualsTo(fullqualifiedName));
            if (@stuct.IsNotNull())
            {
                return (@stuct, reflectionType);
            }


            return (null, reflectionType);
        }
    }
}
