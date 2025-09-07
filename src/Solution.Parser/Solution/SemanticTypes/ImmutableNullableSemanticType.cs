namespace Solution.Parser.Solution
{
    public abstract class ImmutableNullableSemanticType<T>(T value)
    {
        public T Value { get; } = value;
    }
}
