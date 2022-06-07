using System.Diagnostics;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{TypeName}")]
    public class BaseType
    {
        internal BaseType(string type)
        {
            Throw.IfNullOrEmpty(type);

            TypeName = type;
        }

        public string TypeName { get; }
    }
}
