using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>A finalizer, declared as <c>~MyType() { }</c>.</summary>
    [DebuggerDisplay("~{Name}")]
    public record Finalizer : MemberWithBody;
}
