namespace SolutionParser.Common
{
    internal abstract class ImmutableNullableSemanticType<T>
    {
        protected ImmutableNullableSemanticType(T value)
        {
            Value = value;
        }

        internal T Value { get; }
    }
}
