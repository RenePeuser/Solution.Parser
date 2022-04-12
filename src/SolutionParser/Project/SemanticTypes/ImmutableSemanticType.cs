using Argument.Check;

namespace SolutionParser.Project
{
    public abstract class ImmutableSemanticType<T> : ImmutableNullableSemanticType<T>
    {
        protected ImmutableSemanticType(T value) : base(value)
        {
            Throw.IfNull<object>(() => value);
        }
    }
}
