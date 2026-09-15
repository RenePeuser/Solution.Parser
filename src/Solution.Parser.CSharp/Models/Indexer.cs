using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>An indexer, declared as <c>public int this[int i] { get; }</c>.</summary>
    [DebuggerDisplay("this[{Parameters.Count}]")]
    public record Indexer : DeclarationWithModifiers
    {
        public required string Type { get; init; }

        public ImmutableList<Parameter> Parameters { get; init; } = ImmutableList<Parameter>.Empty;

        public PropertyAccessors Accessors { get; init; } = PropertyAccessors.None;

        public bool IsExpressionBodied { get; init; }

        public string? ExplicitInterfaceSpecifier { get; init; }

        public bool IsReadOnly => Accessors.Set is null;

        public bool HasGetter => Accessors.Get is not null;

        public bool HasSetter => Accessors.Set is not null;
    }
}
