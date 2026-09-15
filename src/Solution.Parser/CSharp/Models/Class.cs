using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Class : TypeDeclaration
    {
        public override TypeKind Kind => TypeKind.Class;
    }
}
