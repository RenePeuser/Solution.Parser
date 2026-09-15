using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Type} {Name}")]
    public record Field : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        public Initializer? Initializer { get; init; }

        public bool IsReadOnly => Modifiers.Contains(Modifier.ReadOnly);

        public bool IsConst => Modifiers.Contains(Modifier.Const);

        public bool IsStatic => Modifiers.Contains(Modifier.Static);

        /// <summary>True when the field type itself carries a nullable annotation, as in <c>string?</c>.</summary>
        public bool IsNullable { get; init; }

        /// <summary>
        /// True when the field shares its declaration with others, as <c>_a</c> and <c>_b</c> do in
        /// <c>private int _a, _b;</c>. Each of them is reported as its own field.
        /// </summary>
        public bool IsPartOfMultiVariableDeclaration { get; init; }
    }
}
