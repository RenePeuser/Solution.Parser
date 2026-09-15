using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Struct : TypeDeclaration
    {
        public override TypeKind Kind => TypeKind.Struct;
    }
}
