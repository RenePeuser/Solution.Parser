using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A variable declared inside a body: <c>var x = ...</c>, <c>using var stream = ...</c>, a loop
    /// variable, a caught exception, <c>out var result</c> or <c>is Foo foo</c>.
    /// </summary>
    /// <remarks>
    /// The variables of a member are reported in source order and include those declared inside
    /// lambdas. A local function reports its own, as it does with its calls. Parameters are not
    /// local variables; they are on <see cref="MemberWithBody.Parameters"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// from method in tree.Methods
    /// from variable in method.LocalVariables
    /// where variable.Invocations.Named("GetQueryProvider").Any() &amp;&amp; !variable.IsUsing
    /// select $"{variable.Location}: dispose {variable.Name} with 'using var'";
    /// </code>
    /// </example>
    [DebuggerDisplay("{Type} {Name}")]
    public record LocalVariable : DeclarationBase
    {
        /// <summary>
        /// The type as written; <c>var</c> stays <c>var</c>. Empty when nothing is written, as for
        /// <c>is { } x</c>. See <see cref="TypeReference"/> for the type taken apart.
        /// </summary>
        public required string Type { get; init; }

        public TypeReference TypeReference { get; init; } = TypeReference.None;

        /// <summary>How the variable came into being: a declaration, a loop, a catch, a pattern, ...</summary>
        public LocalVariableKind Kind { get; init; }

        /// <summary>
        /// The value after <c>=</c>, as in <c>var x = Load();</c>. Null when the variable is declared
        /// without one, which is always the case for loop, catch, pattern and out variables.
        /// </summary>
        public string? Initializer { get; init; }

        /// <summary>The initializer classified, see <see cref="Expression"/>; <see cref="Initializer"/> is the same as text.</summary>
        public Expression? InitializerExpression { get; init; }

        /// <summary>The method calls inside <see cref="Initializer"/>, in source order. They are part of the member's <c>Invocations</c> too.</summary>
        public ImmutableList<Invocation> Invocations { get; init; } = ImmutableList<Invocation>.Empty;

        /// <summary>True for <c>using var x = ...;</c> and for the variable of a <c>using (var x = ...)</c> statement.</summary>
        public bool IsUsing { get; init; }

        /// <summary>True for <c>await using</c>, in either form.</summary>
        public bool IsAwaitUsing { get; init; }

        public bool IsConst { get; init; }

        /// <summary>True when the type is <c>var</c> and the compiler infers it.</summary>
        public bool IsVar => TypeReference.IsVar;

        /// <summary>True when the written type carries a <c>?</c> of its own, as in <c>string? name = null;</c>.</summary>
        public bool IsNullable => TypeReference.IsNullable;

        /// <summary>
        /// True when the variable shares its declaration with others, as <c>a</c> and <c>b</c> do in
        /// <c>int a = 1, b = 2;</c>. Each of them is reported on its own.
        /// </summary>
        public bool IsPartOfMultiVariableDeclaration { get; init; }
    }

    public enum LocalVariableKind
    {
        /// <summary><c>var x = 1;</c>, <c>const int X = 1;</c> and <c>using var x = ...;</c>.</summary>
        Declaration,

        /// <summary>The variable of a <c>using (var x = ...)</c> statement.</summary>
        UsingStatement,

        /// <summary>The variable of a <c>for (var i = 0; ...)</c> loop.</summary>
        For,

        /// <summary>The variable of a <c>foreach</c> loop, also each part of <c>foreach (var (key, value) in ...)</c>.</summary>
        ForEach,

        /// <summary>The exception of a <c>catch (Exception ex)</c>.</summary>
        Catch,

        /// <summary>A variable declared by a pattern, as in <c>is Foo foo</c>, <c>case var x</c> or <c>is { } x</c>.</summary>
        Pattern,

        /// <summary>A variable declared in an argument, as in <c>TryParse(text, out var value)</c>.</summary>
        OutArgument,

        /// <summary>A part of <c>var (a, b) = ...</c> or <c>(var a, var b) = ...</c>.</summary>
        Deconstruction,

        /// <summary>The pointer of a <c>fixed (byte* p = ...)</c> statement.</summary>
        Fixed
    }
}
