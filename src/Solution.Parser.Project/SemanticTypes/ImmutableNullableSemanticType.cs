namespace Solution.Parser.Project
{
    public abstract class ImmutableNullableSemanticType<T>(T value)
    {
        public T Value { get; } = value;
    }
}
