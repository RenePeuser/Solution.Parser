namespace Solution.Parser.XAML
{
    public abstract class ImmutableSemanticType<T> : ImmutableNullableSemanticType<T>
    {
        protected ImmutableSemanticType(T value) : base(value)
        {
        }
    }
}
