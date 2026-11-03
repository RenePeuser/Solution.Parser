using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Struct : TypeDeclaration
    {
        public override TypeKind Kind => TypeKind.Struct;

        /// <summary>A <c>ref struct</c>, which lives on the stack only.</summary>
        public bool IsRefStruct => Modifiers.Contains(Modifier.Ref);
    }
}
