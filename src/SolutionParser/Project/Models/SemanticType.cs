using Argument.Check;

namespace SolutionParser.Project
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
