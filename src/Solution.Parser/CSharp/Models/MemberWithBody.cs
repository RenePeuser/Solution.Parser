using System.Collections.Immutable;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The shared shape of every member that can carry executable code.
    /// </summary>
    public abstract record MemberWithBody : DeclarationWithModifiers
    {
        /// <summary>
        /// The block including its braces, or the expression for an expression bodied member. Empty
        /// for an abstract, partial or extern member.
        /// </summary>
        public string Body { get; init; } = string.Empty;

        /// <summary>The statements of the body, one entry per statement, nested blocks kept intact.</summary>
        public ImmutableList<string> Statements { get; init; } = ImmutableList<string>.Empty;

        /// <summary>The non blank source lines of the body.</summary>
        public ImmutableList<string> LineStatements { get; init; } = ImmutableList<string>.Empty;

        public ImmutableList<LocalFunction> LocalFunctions { get; init; } = ImmutableList<LocalFunction>.Empty;

        public ImmutableList<Parameter> Parameters { get; init; } = ImmutableList<Parameter>.Empty;

        /// <summary>True for <c>=&gt; expression</c> rather than a block body.</summary>
        public bool IsExpressionBodied { get; init; }

        /// <summary>True when the body yields, which makes the member an iterator.</summary>
        public bool IsIterator { get; init; }

        /// <summary>True when the member carries the <c>async</c> modifier.</summary>
        public bool IsAsync => Modifiers.Contains(Modifier.Async);

        public bool HasBody => Body.Length > 0;
    }
}
