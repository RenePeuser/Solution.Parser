using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Type} {Name}")]
    public record Field : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        /// <summary>The type taken apart, see <see cref="CSharp.TypeReference"/>; <see cref="Type"/> is the same as text.</summary>
        public TypeReference TypeReference { get; init; } = TypeReference.None;

        public string? Initializer { get; init; }

        /// <summary>The initializer classified, see <see cref="Expression"/>; <see cref="Initializer"/> is the same as text.</summary>
        public Expression? InitializerExpression { get; init; }

        public bool IsReadOnly => Modifiers.Contains(Modifier.ReadOnly);

        public bool IsConst => Modifiers.Contains(Modifier.Const);

        public bool IsVolatile => Modifiers.Contains(Modifier.Volatile);

        /// <summary>A fixed size buffer, as in <c>fixed byte Data[16];</c>.</summary>
        public bool IsFixed => Modifiers.Contains(Modifier.Fixed);

        /// <summary>True when the field type itself carries a nullable annotation, as in <c>string?</c>.</summary>
        public bool IsNullable { get; init; }

        /// <summary>
        /// True when the field shares its declaration with others, as <c>_a</c> and <c>_b</c> do in
        /// <c>private int _a, _b;</c>. Each of them is reported as its own field.
        /// </summary>
        public bool IsPartOfMultiVariableDeclaration { get; init; }
    }
}
