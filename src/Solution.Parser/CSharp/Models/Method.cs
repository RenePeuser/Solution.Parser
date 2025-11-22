using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record LocalFunction(string Name,
                                ImmutableList<Parameter> Parameters,
                                string ReturnParameter,
                                string LocalFunctionValue,
                                string LocalFunctionBody,
                                ImmutableList<string> Statements,
                                ImmutableList<Attribute> Attributes,
                                ImmutableList<Modifier> Modifiers,
                                ImmutableList<string> LineStatements,
                                string SyntaxTree,
                                string FullQualifiedName,
                                string FilePath) : DeclarationWithModifiers(Name, FullQualifiedName, Modifiers,
                                                                            Attributes, SyntaxTree, FilePath);

    public static class LocalFunctionExtensions
    {
        public static bool IsAsync(this LocalFunction method)
        {
            return method.ReturnParameter.Contains("Task", StringComparison.OrdinalIgnoreCase);
        }

        public static bool UseCorrectAsyncNaming(this LocalFunction method)
        {
            return method.Name.EndsWith("Async", StringComparison.Ordinal);
        }
    }
}
