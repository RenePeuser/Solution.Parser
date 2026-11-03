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

        /// <summary>Kept as the previous name for <see cref="CodeElement.SourceCode"/>.</summary>
        public string LocalFunctionValue => SourceCode;

        /// <summary>Kept as the previous name for <see cref="MemberWithBody.Body"/>.</summary>
        public string LocalFunctionBody => Body;
    }
}
