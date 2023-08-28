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
        IImmutableList<ControllerInfo> ExtractFrom(IImmutableList<CSharpSyntaxTree> syntaxTrees,
                                                   IImmutableList<Type> reflectionTypes);
    }

    internal sealed class ControllerParser : IControllerParser
    {
        private readonly MethodParser _methodParser;

        public ControllerParser(MethodParser methodParser)
        {
            _methodParser = methodParser;
        }

        public IImmutableList<ControllerInfo> ExtractFrom(IImmutableList<CSharpSyntaxTree> syntaxTrees,
                                                          IImmutableList<Type> reflectionTypes)
        {
            // 1. Detect all controllers. Derived class like Controller, ControllerBase, ODataController and so on
            var controllers = syntaxTrees.GetAllControllers();

            // 2. Extract all needed infos for generation out
            var controllerInfos = Parse(controllers, reflectionTypes, syntaxTrees).ToImmutableList();

            return controllerInfos;
        }

        private IEnumerable<ControllerInfo> Parse(IImmutableList<Class> controllers,
                                                  IImmutableList<Type> reflectionTypes,
                                                  IImmutableList<CSharpSyntaxTree> syntaxTrees)
        {
            foreach (var controller in controllers)
            {
                // 0. Reflection controller
                var controllerType = reflectionTypes.First(type => type.FullName == controller.FullQualifiedName);

                // 1. Extract meta infos version, base url and son on.
                var version = controller.Attributes.FirstOrDefault(a => a.Name == "ApiVersion")?.Arguments?.FirstOrDefault()?.Trim('"') ?? "1.0";
                var normalizedVersion = $"V{version.Replace(".0", string.Empty) // V1.0 => V1
                                                   .Replace(".", "_")}"; // V1.1 => V1_1}"

                // 2. Get all declared base routes
                var baseUrls = controller.Attributes.Where(a => a.Name == "Route").Select(a => a.Arguments?.FirstOrDefault()?.Replace("{version:apiVersion}", version.ToLowerInvariant()).Trim('"') ?? string.Empty).ToImmutableList();

                // 3. Now parse all methods
                var methodReflection = controllerType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).ToImmutableList();
                var methods = _methodParser.Parse(controller.Methods, baseUrls, methodReflection, syntaxTrees);

                // 4. Return controller infos
                yield return new ControllerInfo
                {
                    Name = controller.Name,
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
        internal static IImmutableList<Class> GetAllControllers(this IImmutableList<CSharpSyntaxTree> syntaxTrees)
        {
            var controllers = (from syntaxTree in syntaxTrees
                               from @class in syntaxTree.Classes
                               where @class.BaseTypes.Any(baseType => baseType.TypeName.Contains("Controller", StringComparison.OrdinalIgnoreCase))
                               select @class).ToImmutableList();

            return controllers;
        }


        internal static (DeclarationBase? Declaration, Type Type) FindDataType(this IImmutableList<CSharpSyntaxTree> syntaxTrees, Type reflectionType)
        {
            var fullqualifiedName = reflectionType.FullName?.Replace("+", "."); // Nested classes have + in reflection full name as separator !
            if (fullqualifiedName.IsNull())
            {
                return (null, reflectionType);
            }

            // Generic types have to be simplified against generic types of solution parser
            fullqualifiedName = fullqualifiedName.Split("`").First();

            // 2.1 Check find class first
            var @class = syntaxTrees.SelectMany(tree => tree.Classes).FirstOrDefault(c => c.FullQualifiedName == fullqualifiedName);
            if (@class.IsNotNull())
            {
                return (@class, reflectionType);
            }

            // 2.2. Check records
            var @record = syntaxTrees.SelectMany(tree => tree.Records).FirstOrDefault(c => c.FullQualifiedName == fullqualifiedName);
            if (@record.IsNotNull())
            {
                return (@record, reflectionType);
            }

            // 2.3. Check enum
            var @enum = syntaxTrees.SelectMany(tree => tree.Enums).FirstOrDefault(c => c.FullQualifiedName == fullqualifiedName);
            if (@enum.IsNotNull())
            {
                return (@enum, reflectionType);
            }

            // 2.3. Check interface
            var @interface = syntaxTrees.SelectMany(tree => tree.Interfaces).FirstOrDefault(c => c.FullQualifiedName == fullqualifiedName);
            if (@interface.IsNotNull())
            {
                return (@interface, reflectionType);
            }

            // 2.4. Check interface
            var @stuct = syntaxTrees.SelectMany(tree => tree.Structs).FirstOrDefault(c => c.FullQualifiedName == fullqualifiedName);
            if (@stuct.IsNotNull())
            {
                return (@stuct, reflectionType);
            }


            return (null, reflectionType);
        }
    }
}
