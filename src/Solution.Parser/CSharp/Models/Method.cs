using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Method(string Name,
                         IImmutableList<Parameter> Parameters,
                         string ReturnParameter,
                         string MethodValue,
                         string MethodBody,
                         IImmutableList<string> Statements,
                         IImmutableList<Attribute> Attributes,
                         IImmutableList<Modifier> Modifiers,
                         IImmutableList<string> LineStatements,
                         string SyntaxTree,
                         string FullQualifiedName,
                         string FilePath) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers, Attributes, SyntaxTree, FilePath);

    public static class MethodExtensions
    {
        public static bool IsAsync(this Method method)
        {
            return method.ReturnParameter.Contains("Task", StringComparison.OrdinalIgnoreCase);
        }

        public static bool UseCorrectAsyncNaming(this Method method)
        {
            return method.Name.EndsWith("Async", StringComparison.Ordinal);
        }
    }

}
