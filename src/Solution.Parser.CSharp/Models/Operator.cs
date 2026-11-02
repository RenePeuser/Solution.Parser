using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>An operator or a conversion operator.</summary>
    [DebuggerDisplay("operator {Name}")]
    public record Operator : MemberWithBody
    {
        /// <summary>The result type. For a conversion operator this is the type converted to.</summary>
        public required string ReturnParameter { get; init; }

        /// <summary>The return type taken apart, see <see cref="TypeReference"/>; <see cref="ReturnParameter"/> is the same as text.</summary>
        public TypeReference ReturnType { get; init; } = TypeReference.None;

        public OperatorKind OperatorKind { get; init; } = OperatorKind.Operator;

        /// <summary>The operator token, for example <c>+</c>, or the target type of a conversion.</summary>
        public required string Symbol { get; init; }

        public bool IsCheckedOperator { get; init; }
    }

    public enum OperatorKind
    {
        Operator,

        ImplicitConversion,

        ExplicitConversion
    }
}
