namespace Solution.Parser.Nuspec
{
    public abstract class ImmutableNullableSemanticType<T>(T value)
    {
        public T Value { get; } = value;
    }
}
