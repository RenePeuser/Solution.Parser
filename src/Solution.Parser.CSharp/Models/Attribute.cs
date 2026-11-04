using System;
using System.Collections.Generic;
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

        /// <summary>The positional arguments classified, see <see cref="Expression"/>; <see cref="PositionalArguments"/> is the same as text.</summary>
        public ImmutableList<Expression> PositionalExpressions { get; init; } = ImmutableList<Expression>.Empty;

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

        /// <summary>
        /// The name of an attribute class as a rule would write it, so <c>ObsoleteAttribute</c> stays
        /// <c>ObsoleteAttribute</c> and the generic <c>JsonConverterAttribute&lt;T&gt;</c> becomes
        /// <c>JsonConverterAttribute</c> instead of the runtime name <c>JsonConverterAttribute`1</c>.
        /// </summary>
        internal static string NameOf<TAttribute>()
            where TAttribute : System.Attribute
        {
            var name = typeof(TAttribute).Name;
            var arity = name.IndexOf('`');

            return arity < 0 ? name : name[..arity];
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
    public record AttributeArgument(string Name, string Value)
    {
        /// <summary>The value classified, see <see cref="Expression"/>; <see cref="Value"/> is the same as text.</summary>
        public Expression? ValueExpression { get; init; }

        /// <summary>The text of a string literal or <c>nameof</c>, without quotes and escapes; null for anything else.</summary>
        public string? StringValue => ValueExpression?.StringValue;
    }

    public static class AttributeListExtensions
    {
        /// <summary>Matches with or without the <c>Attribute</c> suffix, see <see cref="Attribute.IsNamed"/>.</summary>
        public static bool HasAttribute(this ImmutableList<Attribute> attributes, string attributeName)
        {
            return attributes.Any(a => a.IsNamed(attributeName));
        }

        /// <summary>
        /// Like <see cref="HasAttribute(ImmutableList{Attribute}, string)"/> with the name taken from the
        /// attribute class, as in <c>parsed.AssemblyAttributes.HasAttribute&lt;InternalsVisibleToAttribute&gt;()</c>.
        /// </summary>
        public static bool HasAttribute<TAttribute>(this ImmutableList<Attribute> attributes)
            where TAttribute : System.Attribute
        {
            return attributes.HasAttribute(Attribute.NameOf<TAttribute>());
        }

        /// <summary>
        /// The attributes with this name, with or without the <c>Attribute</c> suffix, see
        /// <see cref="Attribute.IsNamed"/>. For a single one, <c>declaration.Attribute(name)</c>.
        /// </summary>
        public static ImmutableList<Attribute> Named(this IEnumerable<Attribute> attributes, string attributeName)
        {
            return attributes.Where(a => a.IsNamed(attributeName)).ToImmutableList();
        }
    }
}
