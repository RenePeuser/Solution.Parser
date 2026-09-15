namespace Solution.Parser.Xaml
{
    public abstract class ImmutableSemanticType<T>(T value) : ImmutableNullableSemanticType<T>(value);
}
