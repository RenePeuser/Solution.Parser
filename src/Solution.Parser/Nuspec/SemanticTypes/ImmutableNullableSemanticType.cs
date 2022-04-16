namespace Solution.Parser.Nuspec
{
    public abstract class ImmutableNullableSemanticType<T>
    {
        protected ImmutableNullableSemanticType(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
