namespace Solution.Parser.Common
{
    internal abstract class ImmutableNullableSemanticType<T>(T value)
    {
        internal T Value { get; } = value;
    }
}
