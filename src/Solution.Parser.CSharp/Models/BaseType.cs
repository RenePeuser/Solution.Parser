using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{TypeName}")]
    public record BaseType(string TypeName)
    {
        /// <summary>The base type taken apart, see <see cref="CSharp.TypeReference"/>; <see cref="TypeName"/> is the same as text.</summary>
        public TypeReference TypeReference { get; init; } = TypeReference.None;
    }
}
