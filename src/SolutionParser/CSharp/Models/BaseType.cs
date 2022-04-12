using Argument.Check;

namespace SolutionParser.CSharp
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
