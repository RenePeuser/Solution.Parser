using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record LocalFunction : MemberWithBody
    {
        public required string ReturnParameter { get; init; }

        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        /// <summary>Kept as the previous name for <see cref="DeclarationBase.SyntaxTree"/>.</summary>
        public string LocalFunctionValue => SyntaxTree;

        /// <summary>Kept as the previous name for <see cref="MemberWithBody.Body"/>.</summary>
        public string LocalFunctionBody => Body;
    }

    public static class LocalFunctionExtensions
    {
        /// <summary>
        /// True when the local function returns an awaitable shaped type. Prefer
        /// <see cref="MemberWithBody.IsAsync"/>, which reports the <c>async</c> modifier.
        /// </summary>
        public static bool IsAsync(this LocalFunction localFunction)
        {
            return localFunction.IsAsync || localFunction.ReturnParameter.Contains("Task", StringComparison.OrdinalIgnoreCase);
        }

        public static bool UseCorrectAsyncNaming(this LocalFunction localFunction)
        {
            return localFunction.Name.EndsWith("Async", StringComparison.Ordinal);
        }
    }
}
