using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Constructor : MemberWithBody
    {
        /// <summary>
        /// The arguments passed to the chained constructor, as in <c>: base(a, b)</c>. Empty when there
        /// is no initializer.
        /// </summary>
        public System.Collections.Immutable.ImmutableList<string> Arguments { get; init; } =
            System.Collections.Immutable.ImmutableList<string>.Empty;

        public ConstructorInitializerKind InitializerKind { get; init; } = ConstructorInitializerKind.None;

        /// <summary>
        /// True for the primary constructor of a record or of a class or struct that declares one. Its
        /// parameters are also reported on <see cref="TypeDeclaration.Parameters"/>.
        /// </summary>
        public bool IsPrimary { get; init; }

        public bool IsStatic => Modifiers.Contains(Modifier.Static);
    }

    public enum ConstructorInitializerKind
    {
        None,

        This,

        Base
    }
}
