using Argument.Check;

namespace Solution.Parser.Sln
{
    public abstract class ImmutableSemanticType<T> : ImmutableNullableSemanticType<T>
    {
        protected ImmutableSemanticType(T value) : base(value)
        {
            Throw.IfNull<object>(value);
        }
    }
}
