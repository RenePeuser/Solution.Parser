using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record LocalFunction : MemberWithBody
    {
        public required string ReturnParameter { get; init; }

        /// <summary>The return type taken apart, see <see cref="TypeReference"/>; <see cref="ReturnParameter"/> is the same as text.</summary>
        public TypeReference ReturnType { get; init; } = TypeReference.None;

        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        public bool IsGeneric => !TypeParameters.IsEmpty;

        /// <summary>Returns <c>void</c>; shortcut for <c>ReturnType.IsVoid</c>.</summary>
        public bool ReturnsVoid => ReturnType.IsVoid;

        /// <summary>
        /// Returns something awaitable: <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c> or <c>ValueTask&lt;T&gt;</c>;
        /// shortcut for <c>ReturnType.IsTask</c>. Says nothing about the <c>async</c> modifier, that is <c>IsAsync</c>.
        /// </summary>
        public bool ReturnsTask => ReturnType.IsTask;

        /// <summary>Kept as the previous name for <see cref="CodeElement.SourceCode"/>.</summary>
        public string LocalFunctionValue => SourceCode;

        /// <summary>Kept as the previous name for <see cref="MemberWithBody.Body"/>.</summary>
        public string LocalFunctionBody => Body;
    }
}
