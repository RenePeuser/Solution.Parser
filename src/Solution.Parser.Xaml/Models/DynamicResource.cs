using System.Diagnostics;

namespace Solution.Parser.Xaml
{
    [DebuggerDisplay("{ResourceKey}")]
    public record DynamicResource : ResourceReference
    {
        internal DynamicResource(string rawValue) : base(rawValue)
        {
        }
    }
}
