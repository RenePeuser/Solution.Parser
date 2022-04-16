using Argument.Check;

namespace Solution.Parser.Project
{
    public abstract class SemanticType<T>
    {
        protected SemanticType(T value)
        {
            Throw.IfNull<object>(() => value);

            Value = value;
        }

        public T Value { get; set; }
    }
}
