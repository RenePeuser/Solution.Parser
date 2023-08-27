using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Extensions.Pack;
using Microsoft.Extensions.DependencyInjection;
using Solution.Parser.CSharp;

namespace Solution.Parser.AspNet
{
    internal static class AddModelNormalizerExtension
    {
        internal static void AddModelNormalizer(this IServiceCollection services)
        {
            services.AddSingletonIfNotExists<ModelNormalizer>();
        }
    }

    internal sealed class ModelNormalizer
    {
        internal IEnumerable<DeclarationBase> Normalize(IImmutableList<DeclarationToType> declarationToTypes)
        {
            // Hint: This is important now, any external data types form external libs have to be used with full
            //       qualified name
            foreach (var declarationToType in declarationToTypes)
            {
                var newDeclarationType = declarationToType.Declaration;
                var properties = declarationToType.Type.GetProperties();
                foreach (var property in properties)
                {
                    // Important generic types, and crazy types like T<T1<T2<T3>>> can be declared 
                    var allTypes = property.PropertyType.GetAllGenericArguments().Concat(property.PropertyType).ToImmutableList();

                    foreach (var type in allTypes)
                    {
                        // If type is system type like string, int nothing to do
                        if (type.IsSystemType())
                        {
                            continue;
                        }

                        // If type is system type like string, int nothing to do
                        if (type.FullName!.Contains("System.") || type.FullName.Contains("Microsoft."))
                        {
                            continue;
                        }


                        // If property type is matching data model use simplified version :) 
                        if (declarationToTypes.Any(declaratonToType => declaratonToType.Declaration.FullQualifiedName == type.FullName))
                        {
                            continue;
                        }

                        var newSyntaxTree = newDeclarationType.SyntaxTree.Replace($"<{type.Name}>", $"<{type.FullName}>", StringComparison.Ordinal);
                        newSyntaxTree = newSyntaxTree.Replace($"private {type.Name}", $"private {type.FullName}", StringComparison.Ordinal);
                        newSyntaxTree = newSyntaxTree.Replace($"internal {type.Name}", $"internal {type.FullName}", StringComparison.Ordinal);
                        newSyntaxTree = newSyntaxTree.Replace($"public {type.Name}", $"public {type.FullName}", StringComparison.Ordinal);
                        newSyntaxTree = newSyntaxTree.Replace($"({type.Name} ", $"({type.FullName} ", StringComparison.Ordinal);
                        newSyntaxTree = newSyntaxTree.ReplaceWholeWord($", {type.Name}", $", {type.FullName} ");

                        // Unknown type like Pulse.Common or external lib so we have to replace it with full qualified name
                        newDeclarationType = newDeclarationType with
                        {
                            SyntaxTree = newSyntaxTree
                        };
                    }
                }

                yield return newDeclarationType;
            }
        }
    }


    internal static class StringExtensions
    {
        internal static string ReplaceWholeWord(this string sourceString, string wordToFind, string replacement, RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            var pattern = $"\\b{wordToFind}\\b";
            return Regex.Replace(sourceString, pattern, replacement, regexOptions);
        }
    }
}
