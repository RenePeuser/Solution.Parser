using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Interface : TypeDeclaration
    {
        public override TypeKind Kind => TypeKind.Interface;
    }
}
