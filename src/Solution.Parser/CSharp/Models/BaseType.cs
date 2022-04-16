using Argument.Check;

namespace Solution.Parser.CSharp
{
    public class BaseType
    {
        internal BaseType(string type)
        {
            Throw.IfNullOrEmpty(() => type);

            TypeName = type;
        }

        public string TypeName { get; }
    }
}
