namespace Solution.Parser.CSharp
{
    /// <summary>
    /// The effective accessibility of a declaration, including the language default that applies
    /// when no accessibility modifier is present.
    /// </summary>
    public enum Accessibility
    {
        /// <summary>Accessibility does not apply, for example for a local function.</summary>
        NotApplicable,

        Private,

        /// <summary><c>private protected</c>.</summary>
        ProtectedAndInternal,

        Protected,

        Internal,

        /// <summary><c>protected internal</c>.</summary>
        ProtectedOrInternal,

        Public
    }
}
