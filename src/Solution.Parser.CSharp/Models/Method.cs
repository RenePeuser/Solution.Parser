using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Method : MemberWithBody
    {
        public required string ReturnParameter { get; init; }

        /// <summary>The return type taken apart, see <see cref="TypeReference"/>; <see cref="ReturnParameter"/> is the same as text.</summary>
        public TypeReference ReturnType { get; init; } = TypeReference.None;

        public ImmutableList<TypeParameter> TypeParameters { get; init; } = ImmutableList<TypeParameter>.Empty;

        /// <summary>The interface in <c>void IDisposable.Dispose()</c>, otherwise null.</summary>
        public string? ExplicitInterfaceSpecifier { get; init; }

        /// <summary>True for the declaring half of a partial method, which has no body.</summary>
        public bool IsPartialDefinition { get; init; }

        /// <summary>
        /// What the member extends: the <c>this</c> parameter of a classic extension method,
        /// <c>Shout(this string value)</c>, or the receiver of the C# 14 extension block the member is
        /// declared in, <c>extension(string value)</c>. Null for a member that extends nothing. A static
        /// member of a block, which extends the type rather than an instance, has a receiver without a
        /// name when the block gives none.
        /// </summary>
        public Parameter? ExtensionReceiver { get; init; }

        /// <summary>True for an extension member, written either way; see <see cref="ExtensionReceiver"/>.</summary>
        public bool IsExtension => ExtensionReceiver is not null;

        /// <summary>True for a member of a C# 14 <c>extension(...) { }</c> block, false for a classic <c>this</c> extension method.</summary>
        public bool IsInExtensionBlock { get; init; }

        public bool IsGeneric => !TypeParameters.IsEmpty;

        public bool IsExplicitInterfaceImplementation => ExplicitInterfaceSpecifier is not null;

        /// <summary>The source of the whole method. Kept as the previous name for <see cref="CodeElement.SourceCode"/>.</summary>
        public string MethodValue => SourceCode;

        /// <summary>Kept as the previous name for <see cref="MemberWithBody.Body"/>.</summary>
        public string MethodBody => Body;
    }
}
