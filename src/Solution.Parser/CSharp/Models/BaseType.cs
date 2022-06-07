using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{TypeName}")]
    public record BaseType(string TypeName);
}
