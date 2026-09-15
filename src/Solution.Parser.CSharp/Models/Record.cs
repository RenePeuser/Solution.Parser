using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{Name}")]
    public record Record : TypeDeclaration
    {
        /// <summary>True for <c>record struct</c>, false for <c>record</c> and <c>record class</c>.</summary>
        public bool IsRecordStruct { get; init; }

        public override TypeKind Kind => IsRecordStruct ? TypeKind.RecordStruct : TypeKind.Record;
    }
}
