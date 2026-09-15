namespace Solution.Parser.Sln
{
    public abstract class ImmutableNullableSemanticType<T>(T value)
    {
        public T Value { get; } = value;
    }
}
