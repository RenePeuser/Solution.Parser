using Argument.Check;

namespace SolutionParser.Common
{
    internal abstract class ImmutableSemanticType<T> : ImmutableNullableSemanticType<T>
    {
        protected ImmutableSemanticType(T value) : base(value)
        {
            Throw.IfNull<object>(() => value);
        }
    }
}
