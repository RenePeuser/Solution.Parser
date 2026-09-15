namespace Solution.Parser.Xaml
{
    public abstract class ImmutableNullableSemanticType<T>(T value)
    {
        public T Value { get; } = value;
    }
}
