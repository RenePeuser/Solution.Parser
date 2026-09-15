using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Method : MemberWithBody
    {
        public required string ReturnParameter { get; init; }

        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        /// <summary>The interface in <c>void IDisposable.Dispose()</c>, otherwise null.</summary>
        public string? ExplicitInterfaceSpecifier { get; init; }

        /// <summary>True for the declaring half of a partial method, which has no body.</summary>
        public bool IsPartialDefinition { get; init; }

        public bool IsGeneric => !TypeParameters.IsEmpty;

        public bool IsExplicitInterfaceImplementation => ExplicitInterfaceSpecifier is not null;

        /// <summary>The source of the whole method. Kept as the previous name for <see cref="DeclarationBase.SyntaxTree"/>.</summary>
        public string MethodValue => SyntaxTree;

        /// <summary>Kept as the previous name for <see cref="MemberWithBody.Body"/>.</summary>
        public string MethodBody => Body;
    }

    public static class MethodExtensions
    {
        /// <summary>
        /// True when the method returns an awaitable shaped type. Prefer <see cref="MemberWithBody.IsAsync"/>,
        /// which reports the <c>async</c> modifier rather than guessing from the return type.
        /// </summary>
        public static bool IsAsync(this Method method)
        {
            return method.IsAsync || method.ReturnParameter.Contains("Task", StringComparison.OrdinalIgnoreCase);
        }

        public static bool UseCorrectAsyncNaming(this Method method)
        {
            return method.Name.EndsWith("Async", StringComparison.Ordinal);
        }
    }
}
