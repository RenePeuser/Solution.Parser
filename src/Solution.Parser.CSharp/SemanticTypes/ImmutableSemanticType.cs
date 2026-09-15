namespace Solution.Parser.CSharp
{
    public abstract record ImmutableSemanticType<T>(T Value) : ImmutableNullableSemanticType<T>(Value);
}
