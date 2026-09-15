using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Attribute : DeclarationBase
    {
        /// <summary>Every argument as written, positional and named alike, in source order.</summary>
        public ImmutableList<string> Arguments { get; init; } = ImmutableList<string>.Empty;

        /// <summary>The arguments given by position, without a <c>Name =</c> or <c>Name:</c> prefix.</summary>
        public ImmutableList<string> PositionalArguments { get; init; } = ImmutableList<string>.Empty;

        /// <summary>The <c>Name = value</c> arguments, keyed by name.</summary>
        public ImmutableList<AttributeArgument> NamedArguments { get; init; } = ImmutableList<AttributeArgument>.Empty;

        /// <summary>The target of the attribute list, for example <c>assembly</c> in <c>[assembly: X]</c>.</summary>
        public string? Target { get; init; }

        /// <summary>
        /// Matches the attribute name with or without the <c>Attribute</c> suffix and with or without a
        /// namespace qualifier, so <c>IsNamed("Obsolete")</c> matches <c>[Obsolete]</c>,
        /// <c>[ObsoleteAttribute]</c> and <c>[System.ObsoleteAttribute]</c> alike.
        /// </summary>
        public bool IsNamed(string attributeName)
        {
            return Simplify(Name).Equals(Simplify(attributeName), StringComparison.Ordinal);
        }

        private static string Simplify(string name)
        {
            var lastDot = name.LastIndexOf('.');
            var unqualified = lastDot < 0 ? name : name[(lastDot + 1)..];

            // Strip a generic argument list, as in [JsonConverter<Foo>].
            var generic = unqualified.IndexOf('<');
            unqualified = generic < 0 ? unqualified : unqualified[..generic];

            return unqualified.EndsWith("Attribute", StringComparison.Ordinal) && unqualified.Length > "Attribute".Length
                       ? unqualified[..^"Attribute".Length]
                       : unqualified;
        }
    }

    [DebuggerDisplay("{Name} = {Value}")]
    public record AttributeArgument(string Name, string Value);

    public static class AttributeListExtensions
    {
        /// <summary>Matches with or without the <c>Attribute</c> suffix, see <see cref="Attribute.IsNamed"/>.</summary>
        public static bool HasAttribute(this ImmutableList<Attribute> attributes, string attributeName)
        {
            return attributes.Any(a => a.IsNamed(attributeName));
        }

        public static Attribute? Named(this ImmutableList<Attribute> attributes, string attributeName)
        {
            return attributes.FirstOrDefault(a => a.IsNamed(attributeName));
        }
    }
}
